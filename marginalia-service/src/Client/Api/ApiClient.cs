using System.Net.Http.Json;
using System.Text.Json;

namespace Marginalia.Api.Client;

public sealed class ApiClient(HttpClient httpClient) : IApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient = httpClient;

    public async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(SerializerOptions, cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(path, body, SerializerOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(SerializerOptions, cancellationToken);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(path, body, SerializerOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(SerializerOptions, cancellationToken);
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
