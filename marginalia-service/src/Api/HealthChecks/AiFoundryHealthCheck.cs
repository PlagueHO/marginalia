using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Marginalia.Api.HealthChecks;

public sealed class AiFoundryHealthCheck(IChatClient? chatClient = null) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (chatClient is null)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "IChatClient not registered — AI analysis unavailable"));
        }

        var metadata = chatClient.GetService<ChatClientMetadata>();
        var description = $"endpoint={metadata?.ProviderUri}, model={metadata?.DefaultModelId}";

        // Verify the endpoint targets a Foundry project, not the account root.
        // Account-level endpoints (e.g. https://x.cognitiveservices.azure.com/)
        // cannot resolve project-scoped model deployments and return 404 at runtime.
        if (metadata?.ProviderUri is { } uri &&
            !uri.AbsolutePath.Contains("/projects/", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Endpoint does not target a Foundry project — model deployments will not resolve. {description}"));
        }

        return Task.FromResult(HealthCheckResult.Healthy($"Configured: {description}"));
    }
}
