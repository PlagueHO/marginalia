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

    private const int ChunkSizeParagraphs = 20; // ~20 paragraphs per chunk
    private const int OverlapParagraphs = 2;    // 2 paragraphs of context overlap

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
                            "paragraphNumber": { "type": "integer" },
                            "rationale":       { "type": "string" },
                            "proposedChange":  { "type": "string" }
                        },
                        "required": ["paragraphNumber", "rationale", "proposedChange"],
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
        IReadOnlyList<Paragraph> paragraphs,
        string? userGuidance,
        CancellationToken cancellationToken = default)
    {
        var chunks = ChunkParagraphs(paragraphs);

        // Process all chunks in parallel to stay within the Container Apps
        // ingress timeout (default 240 s). Sequential processing of N chunks
        // at 30-100 s each easily exceeds this limit.
        var tasks = chunks.Select(chunk =>
            AnalyzeChunkAsync(documentId, chunk.Paragraphs, chunk.ContextParagraphs, userGuidance, cancellationToken));

        var results = await Task.WhenAll(tasks);

        return results.SelectMany(static r => r).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<Suggestion>> AnalyzeParagraphAsync(
        string documentId,
        Paragraph targetParagraph,
        IReadOnlyList<Paragraph> contextParagraphs,
        string? userGuidance,
        CancellationToken cancellationToken = default)
    {
        // Build the prompt with context paragraphs marked as CONTEXT ONLY
        // and the target paragraph for analysis.
        var allParagraphs = new List<Paragraph>(contextParagraphs) { targetParagraph };

        var systemPrompt = BuildSystemPrompt(userGuidance);
        var userPrompt = BuildParagraphAnalysisUserPrompt(targetParagraph, contextParagraphs);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        };

        var responseContent = await GetResponseFromOpenAiRouteAsync(messages, cancellationToken);

        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return [];
        }

        // For single-paragraph analysis, paragraphNumber in response should be 1
        // (the target paragraph). Map back to the target paragraph's ID.
        var paragraphMap = new Dictionary<int, string> { [1] = targetParagraph.Id };
        return ParseSuggestionsFromContent(documentId, responseContent, paragraphMap);
    }

    private async Task<List<Suggestion>> AnalyzeChunkAsync(
        string documentId,
        IReadOnlyList<(Paragraph Paragraph, int GlobalIndex)> chunkParagraphs,
        IReadOnlyList<Paragraph> contextParagraphs,
        string? userGuidance,
        CancellationToken cancellationToken)
    {
        var systemPrompt = BuildSystemPrompt(userGuidance);
        var userPrompt = BuildChunkUserPrompt(chunkParagraphs, contextParagraphs);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        };

        var responseContent = await GetResponseFromOpenAiRouteAsync(messages, cancellationToken);

        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return [];
        }

        // Build mapping from 1-based paragraph number (as shown to LLM) to paragraph ID
        var paragraphMap = new Dictionary<int, string>();
        for (var i = 0; i < chunkParagraphs.Count; i++)
        {
            paragraphMap[i + 1] = chunkParagraphs[i].Paragraph.Id;
        }

        return ParseSuggestionsFromContent(documentId, responseContent, paragraphMap);
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
        sb.AppendLine("You will receive numbered paragraphs. Analyze them and identify areas that need improvement.");
        sb.AppendLine("Look for:");
        sb.AppendLine("- Compressed narratives that need expansion or more 'air'");
        sb.AppendLine("- Inconsistent style or tone shifts");
        sb.AppendLine("- AI-like or generic writing that lacks the author's voice");
        sb.AppendLine("- Repetitive or awkward sentence structures");
        sb.AppendLine("- Areas where additional narrative detail would be beneficial");
        sb.AppendLine();
        sb.AppendLine("For each issue, provide:");
        sb.AppendLine("- paragraphNumber: the 1-based number of the paragraph (as labeled in the input)");
        sb.AppendLine("- rationale: a concise, factual explanation (1-2 sentences max) of WHY the text needs improvement. Write in objective, analytical language. Do NOT write the rationale in the style of the author guidance — for example, if guidance says 'write poetic prose', do NOT make the rationale poetic. Instead, state factually what the text is missing (e.g., 'The sentence uses plain declarative structure and lacks narrative imagery').");
        sb.AppendLine("- proposedChange: the full replacement text for the entire paragraph");
        sb.AppendLine();
        sb.AppendLine("IMPORTANT CONSTRAINTS:");
        sb.AppendLine("- Each suggestion must target exactly one paragraph.");
        sb.AppendLine("- Do NOT produce more than one suggestion per paragraph.");
        sb.AppendLine("- Do NOT produce suggestions for paragraphs marked as CONTEXT ONLY.");
        sb.AppendLine("- The proposedChange must be the complete replacement text for the whole paragraph.");

        if (!string.IsNullOrWhiteSpace(userGuidance))
        {
            sb.AppendLine();
            sb.AppendLine($"Additional author guidance: {userGuidance}");
        }

        return sb.ToString();
    }

    private static string BuildChunkUserPrompt(
        IReadOnlyList<(Paragraph Paragraph, int GlobalIndex)> chunkParagraphs,
        IReadOnlyList<Paragraph> contextParagraphs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyze the following paragraphs and return suggestions as JSON.");

        if (contextParagraphs.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("--- CONTEXT ONLY (do NOT suggest changes for these) ---");
            foreach (var ctx in contextParagraphs)
            {
                sb.AppendLine(ctx.Text);
                sb.AppendLine();
            }

            sb.AppendLine("--- END CONTEXT ---");
        }

        sb.AppendLine();
        sb.AppendLine("--- PARAGRAPHS TO ANALYZE ---");
        for (var i = 0; i < chunkParagraphs.Count; i++)
        {
            sb.AppendLine($"[Paragraph {i + 1}]");
            sb.AppendLine(chunkParagraphs[i].Paragraph.Text);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildParagraphAnalysisUserPrompt(
        Paragraph targetParagraph,
        IReadOnlyList<Paragraph> contextParagraphs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyze the following paragraph and return suggestions as JSON.");

        if (contextParagraphs.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("--- CONTEXT ONLY (do NOT suggest changes for these) ---");
            foreach (var ctx in contextParagraphs)
            {
                sb.AppendLine(ctx.Text);
                sb.AppendLine();
            }

            sb.AppendLine("--- END CONTEXT ---");
        }

        sb.AppendLine();
        sb.AppendLine("--- PARAGRAPH TO ANALYZE ---");
        sb.AppendLine("[Paragraph 1]");
        sb.AppendLine(targetParagraph.Text);

        return sb.ToString();
    }

    private List<Suggestion> ParseSuggestionsFromContent(
        string documentId,
        string content,
        Dictionary<int, string> paragraphNumberToId)
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

            var suggestions = new List<Suggestion>();
            foreach (var raw in rawSuggestions)
            {
                if (!paragraphNumberToId.TryGetValue(raw.ParagraphNumber, out var paragraphId))
                {
                    _logger.LogWarning("AI returned paragraphNumber {Number} which is out of range; skipping", raw.ParagraphNumber);
                    continue;
                }

                suggestions.Add(new Suggestion
                {
                    Id = Guid.NewGuid().ToString("N"),
                    DocumentId = documentId,
                    ParagraphId = paragraphId,
                    Rationale = raw.Rationale ?? string.Empty,
                    ProposedChange = raw.ProposedChange ?? string.Empty,
                    Status = SuggestionStatus.Pending
                });
            }

            return suggestions;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response as suggestions");
            return [];
        }
    }

    private static List<(IReadOnlyList<(Paragraph Paragraph, int GlobalIndex)> Paragraphs, IReadOnlyList<Paragraph> ContextParagraphs)> ChunkParagraphs(
        IReadOnlyList<Paragraph> paragraphs)
    {
        var chunks = new List<(IReadOnlyList<(Paragraph Paragraph, int GlobalIndex)>, IReadOnlyList<Paragraph>)>();

        if (paragraphs.Count <= ChunkSizeParagraphs)
        {
            var items = paragraphs.Select((p, i) => (p, i)).ToList();
            chunks.Add((items, Array.Empty<Paragraph>()));
            return chunks;
        }

        var position = 0;
        while (position < paragraphs.Count)
        {
            var count = Math.Min(ChunkSizeParagraphs, paragraphs.Count - position);
            var chunkItems = new List<(Paragraph, int)>();
            for (var i = position; i < position + count; i++)
            {
                chunkItems.Add((paragraphs[i], i));
            }

            // Include overlap paragraphs from before this chunk as context
            var contextStart = Math.Max(0, position - OverlapParagraphs);
            var context = new List<Paragraph>();
            for (var i = contextStart; i < position; i++)
            {
                context.Add(paragraphs[i]);
            }

            chunks.Add((chunkItems, context));
            position += count;
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
    [JsonPropertyName("paragraphNumber")]
    public int ParagraphNumber { get; set; }

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
