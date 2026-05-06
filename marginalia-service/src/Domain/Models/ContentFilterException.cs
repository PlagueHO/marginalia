using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// Thrown when Azure OpenAI content filtering policies block a request.
/// Contains structured details about which filter categories were triggered.
/// </summary>
public sealed class ContentFilterException : Exception
{
    public IReadOnlyList<ContentFilterResult> FilterResults { get; }

    public ContentFilterException(string message, IReadOnlyList<ContentFilterResult> filterResults)
        : base(message)
    {
        FilterResults = filterResults;
    }

    public ContentFilterException(string message, IReadOnlyList<ContentFilterResult> filterResults, Exception innerException)
        : base(message, innerException)
    {
        FilterResults = filterResults;
    }
}

/// <summary>
/// Represents the result of a single content filter category.
/// </summary>
public sealed record ContentFilterResult
{
    [JsonPropertyName("category")]
    public required string Category { get; init; }

    [JsonPropertyName("filtered")]
    public required bool Filtered { get; init; }

    [JsonPropertyName("severity")]
    public string? Severity { get; init; }
}
