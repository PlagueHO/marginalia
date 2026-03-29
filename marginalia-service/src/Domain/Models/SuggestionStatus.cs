using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// The processing status of a suggestion.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SuggestionStatus
{
    Pending,
    Accepted,
    Rejected,
    Modified
}
