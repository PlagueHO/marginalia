namespace Marginalia.Api.Client;

public interface IApiClient
{
    Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default);
    Task<TResponse?> PostAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken cancellationToken = default);
    Task<TResponse?> PutAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
}
