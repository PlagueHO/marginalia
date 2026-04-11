using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Marginalia.Domain.Configuration;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Marginalia.Infrastructure.Services;

/// <summary>
/// Calls Microsoft Foundry Models endpoint for text analysis and editorial suggestions.
/// Requires IChatClient registered via Aspire Azure AI Inference with DefaultAzureCredential.
/// </summary>
public sealed class FoundrySuggestionService : ISuggestionService
{
    private const string AiScope = "https://ai.azure.com/.default";

    private readonly IChatClient _chatClient;
    private readonly ILogger<FoundrySuggestionService> _logger;
    private readonly IOptionsMonitor<LlmEndpointOptions> _llmOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TokenCredential _tokenCredential;

    private const int ChunkSizeChars = 6000; // ~3 pages of text

    /// <summary>
    /// JSON Schema used with Structured Outputs to guarantee the model returns
    /// a valid <c>{"suggestions": [...]}</c> envelope. Every field is required
    /// and <c>additionalProperties</c> is false, satisfying strict-mode rules.
    /// See: https://learn.microsoft.com/azure/foundry/openai/how-to/structured-outputs
    /// </summary>
    private static readonly BinaryData s_suggestionResponseSchema = BinaryData.FromString("""
        {
            "type": "object",
            "properties": {
                "suggestions": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "start": { "type": "integer" },
                            "end":   { "type": "integer" },
                            "rationale":     { "type": "string" },
                            "proposedChange": { "type": "string" }
                        },
                        "required": ["start", "end", "rationale", "proposedChange"],
                        "additionalProperties": false
                    }
                }
            },
            "required": ["suggestions"],
            "additionalProperties": false
        }
        """);

    public FoundrySuggestionService(
        IChatClient chatClient,
        ILogger<FoundrySuggestionService> logger,
        IOptionsMonitor<LlmEndpointOptions> llmOptions,
        IHttpClientFactory httpClientFactory,
        TokenCredential tokenCredential)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _logger = logger;
        _llmOptions = llmOptions ?? throw new ArgumentNullException(nameof(llmOptions));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _tokenCredential = tokenCredential ?? throw new ArgumentNullException(nameof(tokenCredential));
    }

    public async Task<IReadOnlyList<Suggestion>> AnalyzeAsync(
        string documentId,
        string content,
        string? userGuidance,
        CancellationToken cancellationToken = default)
    {
        var chunks = ChunkText(content);

        // Process all chunks in parallel to stay within the Container Apps
        // ingress timeout (default 240 s). Sequential processing of N chunks
        // at 30-100 s each easily exceeds this limit.
        var tasks = chunks.Select(chunk =>
            AnalyzeChunkAsync(documentId, chunk.Text, chunk.Offset, userGuidance, cancellationToken));

        var results = await Task.WhenAll(tasks);

        return results.SelectMany(static r => r).ToList().AsReadOnly();
    }

    private async Task<List<Suggestion>> AnalyzeChunkAsync(
        string documentId,
        string chunkText,
        int offset,
        string? userGuidance,
        CancellationToken cancellationToken)
    {
        var systemPrompt = BuildSystemPrompt(userGuidance);
        var userPrompt = $"Analyze the following text and return suggestions as a JSON array:\n\n{chunkText}";

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        };

        // Use the OpenAI-compatible route directly. The Azure.AI.Inference SDK
        // sends an API version that the Foundry APIM gateway does not support,
        // so we bypass IChatClient for chat completions and call the endpoint
        // via its standard /openai/v1/chat/completions route.
        var responseContent = await GetResponseFromOpenAiRouteAsync(messages, cancellationToken);

        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return [];
        }

        return ParseSuggestionsFromContent(documentId, responseContent, offset);
    }

    private async Task<string?> GetResponseFromOpenAiRouteAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var endpoint = ResolveOpenAiChatCompletionsEndpoint();
        var modelName = ResolveModelName();

        var token = await _tokenCredential.GetTokenAsync(
            new TokenRequestContext([AiScope]),
            cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

        var payload = new
        {
            model = modelName,
            messages = messages.Select(static message => new
            {
                role = message.Role.Value,
                content = message.Text
            }),
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "suggestion_response",
                    strict = true,
                    schema = JsonSerializer.Deserialize<JsonElement>(s_suggestionResponseSchema)
                }
            }
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var client = _httpClientFactory.CreateClient("foundry-llm");
        using var response = await client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI-compatible fallback failed with status {(int)response.StatusCode}: {content}");
        }

        return ExtractAssistantText(content);
    }

    private Uri ResolveOpenAiChatCompletionsEndpoint()
    {
        var metadataEndpoint = _chatClient.GetService<ChatClientMetadata>()?.ProviderUri;
        var configuredEndpoint = _llmOptions.CurrentValue.Endpoint;
        var endpointCandidate = metadataEndpoint?.ToString() ?? configuredEndpoint;

        if (string.IsNullOrWhiteSpace(endpointCandidate) ||
            !Uri.TryCreate(endpointCandidate, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Unable to resolve AI endpoint for OpenAI-compatible fallback.");
        }

        var builder = new UriBuilder(uri.Scheme, uri.Host)
        {
            Path = "/openai/v1/chat/completions"
        };

        return builder.Uri;
    }

    private string ResolveModelName()
    {
        var metadataModel = _chatClient.GetService<ChatClientMetadata>()?.DefaultModelId;
        var configuredModel = _llmOptions.CurrentValue.ModelName;
        var modelName = metadataModel ?? configuredModel;

        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new InvalidOperationException("Unable to resolve model name for OpenAI-compatible fallback.");
        }

        return modelName;
    }

    private static string? ExtractAssistantText(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);

        if (!document.RootElement.TryGetProperty("choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array ||
            choices.GetArrayLength() == 0)
        {
            return null;
        }

        var firstChoice = choices[0];
        if (!firstChoice.TryGetProperty("message", out var message) ||
            !message.TryGetProperty("content", out var content))
        {
            return null;
        }

        if (content.ValueKind == JsonValueKind.String)
        {
            return content.GetString();
        }

        if (content.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var sb = new StringBuilder();
        foreach (var item in content.EnumerateArray())
        {
            if (!item.TryGetProperty("type", out var type) ||
                !string.Equals(type.GetString(), "text", StringComparison.OrdinalIgnoreCase) ||
                !item.TryGetProperty("text", out var text) ||
                text.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            sb.Append(text.GetString());
        }

        return sb.Length == 0 ? null : sb.ToString();
    }

    private static string BuildSystemPrompt(string? userGuidance)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert editorial assistant for long-form non-fiction manuscripts.");
        sb.AppendLine("Analyze the provided text and identify areas that need improvement.");
        sb.AppendLine("Look for:");
        sb.AppendLine("- Compressed narratives that need expansion or more 'air'");
        sb.AppendLine("- Inconsistent style or tone shifts");
        sb.AppendLine("- AI-like or generic writing that lacks the author's voice");
        sb.AppendLine("- Repetitive or awkward sentence structures");
        sb.AppendLine("- Areas where additional narrative detail would be beneficial");
        sb.AppendLine();
        sb.AppendLine("For each issue, provide:");
        sb.AppendLine("- start: character offset where the issue begins");
        sb.AppendLine("- end: character offset where the issue ends");
        sb.AppendLine("- rationale: clear explanation of why this area needs improvement");
        sb.AppendLine("- proposedChange: the suggested replacement or improvement text");

        if (!string.IsNullOrWhiteSpace(userGuidance))
        {
            sb.AppendLine();
            sb.AppendLine($"Additional author guidance: {userGuidance}");
        }

        return sb.ToString();
    }

    private List<Suggestion> ParseSuggestionsFromContent(string documentId, string content, int offset)
    {
        try
        {
            // Strip markdown fencing if present
            content = content.Trim();
            if (content.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNewline = content.IndexOf('\n');
                if (firstNewline >= 0)
                {
                    content = content[(firstNewline + 1)..];
                }

                if (content.EndsWith("```", StringComparison.Ordinal))
                {
                    content = content[..^3].TrimEnd();
                }
            }

            // Structured Outputs guarantees the response matches our JSON Schema,
            // so we always get {"suggestions": [...]}.
            var wrapper = JsonSerializer.Deserialize(content, FoundrySerializerContext.Default.SuggestionWrapper);
            var rawSuggestions = wrapper?.Suggestions;

            if (rawSuggestions is null)
            {
                return [];
            }

            return rawSuggestions.Select(raw => new Suggestion
            {
                Id = Guid.NewGuid().ToString("N"),
                DocumentId = documentId,
                TextRange = new TextRange
                {
                    Start = raw.Start + offset,
                    End = raw.End + offset
                },
                Rationale = raw.Rationale ?? string.Empty,
                ProposedChange = raw.ProposedChange ?? string.Empty,
                Status = SuggestionStatus.Pending
            }).ToList();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response as suggestions");
            return [];
        }
    }

    private static List<(string Text, int Offset)> ChunkText(string content)
    {
        var chunks = new List<(string Text, int Offset)>();

        if (content.Length <= ChunkSizeChars)
        {
            chunks.Add((content, 0));
            return chunks;
        }

        var position = 0;
        while (position < content.Length)
        {
            var remaining = content.Length - position;
            var length = Math.Min(ChunkSizeChars, remaining);

            // Try to break on paragraph boundary
            if (length < remaining)
            {
                var breakPoint = content.LastIndexOf("\n\n", position + length, length, StringComparison.Ordinal);
                if (breakPoint > position)
                {
                    length = breakPoint - position + 2;
                }
                else
                {
                    var lineBreak = content.LastIndexOf('\n', position + length, length);
                    if (lineBreak > position)
                    {
                        length = lineBreak - position + 1;
                    }
                }
            }

            chunks.Add((content.Substring(position, length), position));
            position += length;
        }

        return chunks;
    }
}

// Internal DTOs for parsing AI response content

internal sealed class SuggestionWrapper
{
    [JsonPropertyName("suggestions")]
    public List<RawSuggestion>? Suggestions { get; set; }
}

internal sealed class RawSuggestion
{
    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("end")]
    public int End { get; set; }

    [JsonPropertyName("rationale")]
    public string? Rationale { get; set; }

    [JsonPropertyName("proposedChange")]
    public string? ProposedChange { get; set; }
}

/// <summary>
/// Source generation context for System.Text.Json serialization of AI response types.
/// </summary>
[JsonSerializable(typeof(SuggestionWrapper))]
internal sealed partial class FoundrySerializerContext : JsonSerializerContext;
