using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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

        var payload = new
        {
            model = _metadata.DefaultModelId,
            messages = chatMessages.Select(static message => new
            {
                role = message.Role.Value,
                content = message.Text
            }),
            temperature = options?.Temperature
        };

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
            if (!item.TryGetProperty("type", out var type) ||
                !string.Equals(type.GetString(), "text", StringComparison.OrdinalIgnoreCase) ||
                !item.TryGetProperty("text", out var text) ||
                text.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            builder.Append(text.GetString());
        }

        return builder.Length > 0
            ? builder.ToString()
            : throw new InvalidOperationException("The assistant content did not contain any text parts.");
    }
}
