using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Marginalia.Api.HealthChecks;

public sealed class CosmosDbHealthCheck(CosmosClient cosmosClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await cosmosClient.ReadAccountAsync();
            return HealthCheckResult.Healthy($"Connected to {account.Id}");
        }
        catch (Exception ex)
        {
            // Unwrap aggregate exceptions from DefaultAzureCredential to surface root cause
            var rootMessage = ex is AuthenticationFailedException or CredentialUnavailableException
                ? ex.Message
                : ex.InnerException?.Message ?? ex.Message;

            return HealthCheckResult.Unhealthy(
                rootMessage.Length > 500 ? rootMessage[..500] : rootMessage,
                ex);
        }
    }
}
