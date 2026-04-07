using System.Net;
using FluentAssertions;
using Marginalia.Api.Middleware;
using Marginalia.Domain.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Marginalia.Tests.Unit.Middleware;

[TestClass]
[TestCategory("Unit")]
public sealed class AccessCodeMiddlewareTests
{
    private IOptionsMonitor<AccessControlOptions> _optionsMonitor = null!;
    private ILogger<AccessCodeMiddleware> _logger = null!;
    private bool _nextCalled;

    [TestInitialize]
    public void Setup()
    {
        _optionsMonitor = Substitute.For<IOptionsMonitor<AccessControlOptions>>();
        _logger = NullLogger<AccessCodeMiddleware>.Instance;
        _nextCalled = false;
    }

    private RequestDelegate CreateNext()
    {
        return _ =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        };
    }

    private AccessCodeMiddleware CreateMiddleware()
    {
        return new AccessCodeMiddleware(CreateNext(), _logger);
    }

    [TestMethod]
    public async Task InvokeAsync_NoAccessCodeConfigured_PassesThrough()
    {
        _optionsMonitor.CurrentValue.Returns(new AccessControlOptions { AccessCode = null });
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context, _optionsMonitor);

        _nextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_EmptyAccessCodeConfigured_PassesThrough()
    {
        _optionsMonitor.CurrentValue.Returns(new AccessControlOptions { AccessCode = "" });
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context, _optionsMonitor);

        _nextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_ValidAccessCode_PassesThrough()
    {
        _optionsMonitor.CurrentValue.Returns(new AccessControlOptions { AccessCode = "secret123" });
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Access-Code"] = "secret123";
        context.Request.Path = "/api/documents";

        await middleware.InvokeAsync(context, _optionsMonitor);

        _nextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_MissingHeader_Returns401()
    {
        _optionsMonitor.CurrentValue.Returns(new AccessControlOptions { AccessCode = "secret123" });
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/documents";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, _optionsMonitor);

        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task InvokeAsync_InvalidAccessCode_Returns401()
    {
        _optionsMonitor.CurrentValue.Returns(new AccessControlOptions { AccessCode = "secret123" });
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Access-Code"] = "wrong-code";
        context.Request.Path = "/api/documents";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, _optionsMonitor);

        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task InvokeAsync_Returns401_WithJsonErrorBody()
    {
        _optionsMonitor.CurrentValue.Returns(new AccessControlOptions { AccessCode = "secret123" });
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/documents";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, _optionsMonitor);

        context.Response.ContentType.Should().Be("application/json");
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().Contain("Access code required");
    }

    [TestMethod]
    [DataRow("/health")]
    [DataRow("/alive")]
    [DataRow("/api/config/access-status")]
    [DataRow("/api/error")]
    [DataRow("/openapi/v1.json")]
    public async Task InvokeAsync_AllowlistedPath_PassesThroughWithoutCode(string path)
    {
        _optionsMonitor.CurrentValue.Returns(new AccessControlOptions { AccessCode = "secret123" });
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        await middleware.InvokeAsync(context, _optionsMonitor);

        _nextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_AllowlistedPath_CaseInsensitive()
    {
        _optionsMonitor.CurrentValue.Returns(new AccessControlOptions { AccessCode = "secret123" });
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Path = "/Health";

        await middleware.InvokeAsync(context, _optionsMonitor);

        _nextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_ProtectedPath_RequiresCode()
    {
        _optionsMonitor.CurrentValue.Returns(new AccessControlOptions { AccessCode = "secret123" });
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/documents";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, _optionsMonitor);

        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
    }
}
