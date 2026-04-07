using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Marginalia.Domain.Configuration;
using Microsoft.Extensions.Options;

namespace Marginalia.Api.Middleware;

/// <summary>
/// Middleware that enforces access code protection when <see cref="AccessControlOptions.AccessCode"/> is configured.
/// Requests to allowlisted paths (health, access status, error, OpenAPI) bypass the check.
/// </summary>
public sealed class AccessCodeMiddleware
{
    private static readonly string[] AllowlistedPathPrefixes =
    [
        "/health",
        "/alive",
        "/api/config/access-status",
        "/api/error",
        "/openapi",
    ];

    private readonly RequestDelegate _next;
    private readonly ILogger<AccessCodeMiddleware> _logger;

    public AccessCodeMiddleware(RequestDelegate next, ILogger<AccessCodeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IOptionsMonitor<AccessControlOptions> options)
    {
        var accessCode = options.CurrentValue.AccessCode;

        // No access code configured — pass through
        if (string.IsNullOrEmpty(accessCode))
        {
            await _next(context);
            return;
        }

        // Allowlisted paths bypass the check
        var path = context.Request.Path.Value ?? "";
        if (IsAllowlistedPath(path))
        {
            await _next(context);
            return;
        }

        // Validate the X-Access-Code header
        var providedCode = context.Request.Headers["X-Access-Code"].ToString();
        if (string.IsNullOrEmpty(providedCode) || !FixedTimeEquals(accessCode, providedCode))
        {
            _logger.LogWarning("Access code validation failed for {Method} {Path}", context.Request.Method, path);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { error = "Access code required" }),
                context.RequestAborted);
            return;
        }

        await _next(context);
    }

    private static bool IsAllowlistedPath(string path)
    {
        foreach (var prefix in AllowlistedPathPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
