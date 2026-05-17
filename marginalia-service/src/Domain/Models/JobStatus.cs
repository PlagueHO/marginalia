using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
