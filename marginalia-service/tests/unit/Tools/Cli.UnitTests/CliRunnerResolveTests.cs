using FluentAssertions;
using Marginalia.Tools.Cli;

namespace Marginalia.Tools.Cli.UnitTests;

[TestClass]
[TestCategory("Unit")]
[DoNotParallelize] // Tests modify shared process environment variables; must run serially to avoid race conditions.
public sealed class CliRunnerResolveTests
{
    [TestInitialize]
    public void Setup()
    {
        Environment.SetEnvironmentVariable("MARGINALIA_API_URL", null);
        Environment.SetEnvironmentVariable("MARGINALIA_USER_ID", null);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Environment.SetEnvironmentVariable("MARGINALIA_API_URL", null);
        Environment.SetEnvironmentVariable("MARGINALIA_USER_ID", null);
    }

    [TestMethod]
    public void ResolveApiUrl_WithExplicitOverride_ReturnsOverride()
    {
        var result = CliRunner.ResolveApiUrl("http://localhost:5000");

        result.Should().Be("http://localhost:5000");
    }

    [TestMethod]
    public void ResolveApiUrl_WithEnvironmentVariable_ReturnsEnvVarValue()
    {
        Environment.SetEnvironmentVariable("MARGINALIA_API_URL", "http://env-host:5001");

        var result = CliRunner.ResolveApiUrl(null);

        result.Should().Be("http://env-host:5001");
    }

    [TestMethod]
    public void ResolveApiUrl_WhenNoUrlAvailable_ThrowsInvalidOperationException()
    {
        var act = () => CliRunner.ResolveApiUrl(null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*--api-url*MARGINALIA_API_URL*");
    }

    [TestMethod]
    public void ResolveApiUrl_OverrideTakesPrecedenceOverEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable("MARGINALIA_API_URL", "http://env-host:5001");

        var result = CliRunner.ResolveApiUrl("http://override:9000");

        result.Should().Be("http://override:9000");
    }

    [TestMethod]
    public void ResolveUserId_WithExplicitOverride_ReturnsOverride()
    {
        var result = CliRunner.ResolveUserId("user-123");

        result.Should().Be("user-123");
    }

    [TestMethod]
    public void ResolveUserId_WithEnvironmentVariable_ReturnsEnvVarValue()
    {
        Environment.SetEnvironmentVariable("MARGINALIA_USER_ID", "env-user-456");

        var result = CliRunner.ResolveUserId(null);

        result.Should().Be("env-user-456");
    }

    [TestMethod]
    public void ResolveUserId_WhenNoValueAvailable_ReturnsNull()
    {
        var result = CliRunner.ResolveUserId(null);

        result.Should().BeNull();
    }

    [TestMethod]
    public void ResolveUserId_OverrideTakesPrecedenceOverEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable("MARGINALIA_USER_ID", "env-user-456");

        var result = CliRunner.ResolveUserId("explicit-user");

        result.Should().Be("explicit-user");
    }
}
