using FluentAssertions;
using Marginalia.Api.HealthChecks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Marginalia.Tests.Unit.HealthChecks;

[TestClass]
[TestCategory("Unit")]
public sealed class CosmosDbHealthCheckTests
{
    private CosmosClient _cosmosClient = null!;
    private CosmosDbHealthCheck _healthCheck = null!;

    [TestInitialize]
    public void Setup()
    {
        _cosmosClient = Substitute.For<CosmosClient>();
        _healthCheck = new CosmosDbHealthCheck(_cosmosClient);
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenConnected_ReturnsHealthy()
    {
        var account = (AccountProperties)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(AccountProperties));
        _cosmosClient.ReadAccountAsync().Returns(account);

        var result = await _healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().StartWith("Connected to");
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenConnectionFails_ReturnsUnhealthy()
    {
        _cosmosClient.ReadAccountAsync()
            .ThrowsAsync(new CosmosException("Service unavailable", System.Net.HttpStatusCode.ServiceUnavailable, 0, "", 0));

        var result = await _healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<CosmosException>();
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenAuthFails_ReturnsUnhealthyWithRootMessage()
    {
        var innerException = new InvalidOperationException("Credential unavailable");
        var outerException = new Exception("Outer wrapper", innerException);
        _cosmosClient.ReadAccountAsync().ThrowsAsync(outerException);

        var result = await _healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Credential unavailable");
    }

    [TestMethod]
    public async Task CheckHealthAsync_TruncatesLongErrorMessages()
    {
        var longMessage = new string('x', 600);
        _cosmosClient.ReadAccountAsync()
            .ThrowsAsync(new Exception(longMessage));

        var result = await _healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().HaveLength(500);
    }
}
