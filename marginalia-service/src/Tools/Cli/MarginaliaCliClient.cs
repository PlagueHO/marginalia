using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marginalia.Tools.Cli.Models;

namespace Marginalia.Tools.Cli;

internal sealed class MarginaliaCliClient : IMarginaliaCliClient
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public MarginaliaCliClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> StartImportAsync(string filePath, bool overwrite, CancellationToken cancellationToken)
    {
        await using var fileStream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        var url = overwrite ? "api/imports?overwrite=true" : "api/imports";
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JobIdResponse>(JsonOptions, cancellationToken);
        return result!.JobId;
    }

    public async Task<JobStatusResponse> GetImportJobAsync(string jobId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"api/imports/{Uri.EscapeDataString(jobId)}",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JobStatusResponse>(JsonOptions, cancellationToken))!;
    }

    public async Task<string> StartExportAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync("api/exports", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JobIdResponse>(JsonOptions, cancellationToken);
        return result!.JobId;
    }

    public async Task<JobStatusResponse> GetExportJobAsync(string jobId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"api/exports/{Uri.EscapeDataString(jobId)}",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JobStatusResponse>(JsonOptions, cancellationToken))!;
    }

    public async Task DownloadExportAsync(string jobId, string outputPath, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"api/exports/{Uri.EscapeDataString(jobId)}/download",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var fileStream = File.Create(outputPath);
        await response.Content.CopyToAsync(fileStream, cancellationToken);
    }

    private sealed record JobIdResponse
    {
        [JsonPropertyName("jobId")]
        public required string JobId { get; init; }
    }
}
