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
        return Task.FromResult(HealthCheckResult.Healthy(
            $"Configured: endpoint={metadata?.ProviderUri}, model={metadata?.DefaultModelId}"));
    }
}
