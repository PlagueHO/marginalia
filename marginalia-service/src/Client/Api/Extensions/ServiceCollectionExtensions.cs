namespace Marginalia.Api.Client.Extensions;

using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMarginaliaApiClient(
        this IServiceCollection services,
        Action<ApiClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ApiClientOptions { BaseUrl = string.Empty };
        configure(options);

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new ArgumentException("BaseUrl must be configured.", nameof(configure));
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("BaseUrl must be a valid absolute HTTP or HTTPS URI.", nameof(configure));
        }

        var builder = services.AddHttpClient<IApiClient, ApiClient>(client =>
        {
            client.BaseAddress = new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/");
        });

        if (options.RequestTimeout.HasValue)
        {
            builder.AddStandardResilienceHandler(resilience =>
            {
                resilience.TotalRequestTimeout.Timeout = options.RequestTimeout.Value;
                resilience.AttemptTimeout.Timeout = options.RequestTimeout.Value;
                resilience.CircuitBreaker.SamplingDuration = options.RequestTimeout.Value * 2;
            });
        }
        else
        {
            builder.AddStandardResilienceHandler();
        }

        return services;
    }
}
