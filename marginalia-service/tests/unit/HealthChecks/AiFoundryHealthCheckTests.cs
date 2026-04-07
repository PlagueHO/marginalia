using FluentAssertions;
using Marginalia.Api.HealthChecks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;

namespace Marginalia.Tests.Unit.HealthChecks;

[TestClass]
[TestCategory("Unit")]
public sealed class AiFoundryHealthCheckTests
{
    [TestMethod]
    public async Task CheckHealthAsync_WhenChatClientNull_ReturnsDegraded()
    {
        var healthCheck = new AiFoundryHealthCheck(chatClient: null);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("IChatClient not registered");
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenChatClientRegistered_ReturnsHealthy()
    {
        var chatClient = Substitute.For<IChatClient>();
        var metadata = new ChatClientMetadata(
            providerName: "test",
            providerUri: new Uri("https://ai.example.com/api/projects/myproject"),
            defaultModelId: "gpt-4o");
        chatClient.GetService<ChatClientMetadata>().Returns(metadata);

        var healthCheck = new AiFoundryHealthCheck(chatClient);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("https://ai.example.com/api/projects/myproject");
        result.Description.Should().Contain("gpt-4o");
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenEndpointMissingProjectPath_ReturnsUnhealthy()
    {
        var chatClient = Substitute.For<IChatClient>();
        var metadata = new ChatClientMetadata(
            providerName: "test",
            providerUri: new Uri("https://ai.example.com/"),
            defaultModelId: "gpt-4o");
        chatClient.GetService<ChatClientMetadata>().Returns(metadata);

        var healthCheck = new AiFoundryHealthCheck(chatClient);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("does not target a Foundry project");
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenChatClientHasNoMetadata_ReturnsHealthy()
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient.GetService<ChatClientMetadata>().Returns((ChatClientMetadata?)null);

        var healthCheck = new AiFoundryHealthCheck(chatClient);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
    }
}
