using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Marginalia.Infrastructure.Services;

/// <summary>
/// Calls Microsoft Foundry Models endpoint for text analysis and editorial suggestions.
/// Requires IChatClient registered via Aspire Azure AI Inference with DefaultAzureCredential.
/// </summary>
public sealed class FoundrySuggestionService : ISuggestionService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<FoundrySuggestionService> _logger;

    private const int ChunkSizeChars = 6000; // ~3 pages of text

    public FoundrySuggestionService(
        IChatClient chatClient,
        ILogger<FoundrySuggestionService> logger)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _logger = logger;
    }

    public async Task<IReadOnlyList<Suggestion>> AnalyzeAsync(
        string documentId,
        string content,
        string? userGuidance,
        CancellationToken cancellationToken = default)
    {
        var chunks = ChunkText(content);
        var allSuggestions = new List<Suggestion>();

        for (var i = 0; i < chunks.Count; i++)
        {
            var (chunkText, offset) = chunks[i];
            var suggestions = await AnalyzeChunkAsync(documentId, chunkText, offset, userGuidance, cancellationToken);
            allSuggestions.AddRange(suggestions);
        }

        return allSuggestions.AsReadOnly();
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
        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        var responseContent = response.Text;

        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return [];
        }

        return ParseSuggestionsFromContent(documentId, responseContent, offset);
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
        sb.AppendLine("Return a JSON array of suggestion objects. Each object must have:");
        sb.AppendLine("- \"start\": character offset where the issue begins (integer)");
        sb.AppendLine("- \"end\": character offset where the issue ends (integer)");
        sb.AppendLine("- \"rationale\": clear explanation of why this area needs improvement");
        sb.AppendLine("- \"proposedChange\": the suggested replacement or improvement text");
        sb.AppendLine();
        sb.AppendLine("Return ONLY the JSON array, no markdown fencing or extra text.");

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

            var rawSuggestions = JsonSerializer.Deserialize(content, FoundrySerializerContext.Default.ListRawSuggestion);

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

// Internal DTO for parsing AI response content

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
[JsonSerializable(typeof(List<RawSuggestion>))]
internal sealed partial class FoundrySerializerContext : JsonSerializerContext;
