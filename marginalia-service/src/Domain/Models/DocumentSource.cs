using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// The source from which a document was ingested.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DocumentSource
{
    Local,
    GoogleDocs
}
