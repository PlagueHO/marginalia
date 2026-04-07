using Marginalia.Domain.Configuration;
using Marginalia.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Marginalia.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ConfigController : ControllerBase
{
    private readonly IOptionsMonitor<LlmEndpointOptions> _llmOptions;
    private readonly IOptionsMonitor<AccessControlOptions> _accessControlOptions;
    private readonly IChatClient? _chatClient;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(
        IOptionsMonitor<LlmEndpointOptions> llmOptions,
        IOptionsMonitor<AccessControlOptions> accessControlOptions,
        ILogger<ConfigController> logger,
        IChatClient? chatClient = null)
    {
        _llmOptions = llmOptions;
        _accessControlOptions = accessControlOptions;
        _chatClient = chatClient;
        _logger = logger;
    }

    /// <summary>
    /// Check whether the application requires an access code.
    /// This endpoint is not protected by the access code middleware.
    /// </summary>
    [HttpGet("access-status")]
    public ActionResult<AccessControlStatusResponse> GetAccessStatus()
    {
        var accessCodeRequired = !string.IsNullOrEmpty(_accessControlOptions.CurrentValue.AccessCode);

        _logger.LogInformation("Access status requested — AccessCodeRequired: {AccessCodeRequired}", accessCodeRequired);

        return Ok(new AccessControlStatusResponse
        {
            AccessCodeRequired = accessCodeRequired
        });
    }

    /// <summary>
    /// Get current LLM endpoint configuration. Authentication is always Entra ID via Aspire.
    /// Uses IChatClient metadata for real values when available, falls back to LlmEndpointOptions.
    /// </summary>
    [HttpGet("llm")]
    public ActionResult<LlmConfigResponse> GetLlmConfig()
    {
        var current = _llmOptions.CurrentValue;
        var isConfigured = _chatClient is not null;

        var metadata = _chatClient?.GetService<ChatClientMetadata>();
        var endpoint = metadata?.ProviderUri?.ToString() ?? current.Endpoint;
        var modelName = metadata?.DefaultModelId ?? current.ModelName;

        _logger.LogInformation("LLM config requested — IsConfigured: {IsConfigured}, AuthMethod: {AuthMethod}, MetadataAvailable: {MetadataAvailable}",
            isConfigured, "entraId", metadata is not null);

        return Ok(new LlmConfigResponse
        {
            Endpoint = endpoint,
            ModelName = modelName,
            IsConfigured = isConfigured,
            AuthMethod = "entraId"
        });
    }

    /// <summary>
    /// Check whether the backend has a live connection to Azure AI Foundry.
    /// </summary>
    [HttpGet("llm/health")]
    public ActionResult<LlmHealthResponse> CheckHealth()
    {
        var isHealthy = _chatClient is not null;
        var message = isHealthy
            ? "Connected to Azure AI Foundry via Entra ID"
            : "AI Foundry client not configured — check Aspire connection string";

        if (isHealthy)
        {
            _logger.LogInformation("Health check passed: {Message}", message);
        }
        else
        {
            _logger.LogWarning("Health check failed: {Message}", message);
        }

        return Ok(new LlmHealthResponse
        {
            Healthy = isHealthy,
            Message = message
        });
    }
}
