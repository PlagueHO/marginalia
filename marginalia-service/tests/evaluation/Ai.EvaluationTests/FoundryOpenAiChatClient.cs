using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.Core;
using Marginalia.Domain.Models;
using Microsoft.Extensions.AI;

namespace Marginalia.Ai.EvaluationTests;

internal sealed class FoundryOpenAiChatClient : IChatClient
{
    private const string AiScope = "https://ai.azure.com/.default";

    private readonly HttpClient _httpClient;
    private readonly TokenCredential _tokenCredential;
    private readonly ChatClientMetadata _metadata;

    public FoundryOpenAiChatClient(HttpClient httpClient, TokenCredential tokenCredential, Uri providerUri, string modelName)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tokenCredential = tokenCredential ?? throw new ArgumentNullException(nameof(tokenCredential));
        _metadata = new ChatClientMetadata(
            providerName: "FoundryOpenAI",
            providerUri: providerUri,
            defaultModelId: modelName);
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var token = await _tokenCredential.GetTokenAsync(
            new TokenRequestContext([AiScope]),
            cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildChatCompletionsEndpoint(_metadata.ProviderUri));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        var payload = new Dictionary<string, object?>
        {
            ["model"] = ResolveModelId(options),
            ["messages"] = chatMessages.Select(static message => new Dictionary<string, object?>
            {
                ["role"] = message.Role.Value,
                ["content"] = message.Text
            }).ToArray()
        };

        if (options?.Temperature is float temperature)
        {
            payload["temperature"] = temperature;
        }

        if (options?.MaxOutputTokens is int maxOutputTokens)
        {
            payload["max_completion_tokens"] = maxOutputTokens;
        }

        if (options?.TopP is float topP)
        {
            payload["top_p"] = topP;
        }

        if (options?.FrequencyPenalty is float frequencyPenalty)
        {
            payload["frequency_penalty"] = frequencyPenalty;
        }

        if (options?.PresencePenalty is float presencePenalty)
        {
            payload["presence_penalty"] = presencePenalty;
        }

        if (options?.Seed is long seed)
        {
            payload["seed"] = seed;
        }

        if (options?.StopSequences is { Count: > 0 } stopSequences)
        {
            payload["stop"] = stopSequences.ToArray();
        }

        var responseFormat = BuildResponseFormat(options);
        if (responseFormat is not null)
        {
            payload["response_format"] = responseFormat;
        }

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Foundry chat completion failed with status {(int)response.StatusCode}: {content}");
        }

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, ExtractAssistantText(content)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Streaming responses are not required for the evaluation suite.");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(ChatClientMetadata) ? _metadata : null;

    public void Dispose()
    {
    }

    private static Uri BuildChatCompletionsEndpoint(Uri? providerUri)
    {
        if (providerUri is null)
        {
            throw new InvalidOperationException("A Foundry provider URI is required to build the chat completions endpoint.");
        }

        var builder = new UriBuilder(providerUri.Scheme, providerUri.Host)
        {
            Path = "/openai/v1/chat/completions"
        };

        return builder.Uri;
    }

    private string ResolveModelId(ChatOptions? options)
    {
        if (!string.IsNullOrWhiteSpace(options?.ModelId))
        {
            return options.ModelId;
        }

        if (!string.IsNullOrWhiteSpace(_metadata.DefaultModelId))
        {
            return _metadata.DefaultModelId;
        }

        throw new InvalidOperationException("A model deployment name is required for the Foundry evaluation chat client.");
    }

    private static object? BuildResponseFormat(ChatOptions? options)
    {
        var responseFormat = options?.ResponseFormat;
        if (responseFormat is null || responseFormat is ChatResponseFormatText)
        {
            return null;
        }

        if (responseFormat is not ChatResponseFormatJson jsonResponseFormat)
        {
            throw new NotSupportedException(
                $"Response format '{responseFormat.GetType().FullName}' is not supported by the Foundry evaluation chat client.");
        }

        if (jsonResponseFormat.Schema is JsonElement schema)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = "json_schema",
                ["json_schema"] = new Dictionary<string, object?>
                {
                    ["name"] = NormalizeSchemaName(jsonResponseFormat.SchemaName),
                    ["description"] = jsonResponseFormat.SchemaDescription,
                    ["strict"] = true,
                    ["schema"] = schema
                }
            };
        }

        return new Dictionary<string, object?>
        {
            ["type"] = "json_object"
        };
    }

    private static string NormalizeSchemaName(string? schemaName)
    {
        var candidate = string.IsNullOrWhiteSpace(schemaName) ? "response" : schemaName.Trim();
        var normalized = Regex.Replace(candidate, "[^A-Za-z0-9_-]", "_");
        return string.IsNullOrWhiteSpace(normalized) ? "response" : normalized;
    }

    private static string ExtractAssistantText(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        if (!document.RootElement.TryGetProperty("choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array ||
            choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("The Foundry chat completion response did not contain any choices.");
        }

        var firstChoice = choices[0];
        if (!firstChoice.TryGetProperty("message", out var message) ||
            !message.TryGetProperty("content", out var content))
        {
            throw new InvalidOperationException("The Foundry chat completion response did not contain assistant content.");
        }

        if (content.ValueKind == JsonValueKind.String)
        {
            return content.GetString() ?? string.Empty;
        }

        if (content.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("The assistant content format was not recognized.");
        }

        var builder = new StringBuilder();
        foreach (var item in content.EnumerateArray())
        {
            if (!TryGetTextContent(item, out var text))
            {
                continue;
            }

            builder.Append(text);
        }

        return builder.Length > 0
            ? builder.ToString()
            : throw new InvalidOperationException("The assistant content did not contain any text parts.");
    }

    private static bool TryGetTextContent(JsonElement contentPart, out string text)
    {
        text = string.Empty;

        if (!contentPart.TryGetProperty("text", out var textElement) ||
            textElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        if (contentPart.TryGetProperty("type", out var typeElement))
        {
            var type = typeElement.GetString();
            if (!string.Equals(type, "text", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(type, "output_text", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        text = textElement.GetString() ?? string.Empty;
        return text.Length > 0;
    }
}
