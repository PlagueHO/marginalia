using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Marginalia.Api.HealthChecks;

public sealed class ManagedIdentityHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var credential = new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(["https://management.azure.com/.default"]),
                cancellationToken);

            return HealthCheckResult.Healthy($"Token acquired, expires {token.ExpiresOn:u}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message, ex);
        }
    }
}
