namespace Marginalia.Api.Client;

public sealed class ApiClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public TimeSpan? RequestTimeout { get; set; }
}
