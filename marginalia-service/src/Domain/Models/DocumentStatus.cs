using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// The processing status of a document.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DocumentStatus
{
    Draft,
    Analyzed
}
