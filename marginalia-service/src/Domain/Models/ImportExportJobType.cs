using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImportExportJobType
{
    Export = 0,
    Import = 1
}
