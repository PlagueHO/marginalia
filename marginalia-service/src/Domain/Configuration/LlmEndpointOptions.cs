namespace Marginalia.Domain.Configuration;

/// <summary>
/// Configuration for the LLM endpoint. Authentication is exclusively via Entra ID
/// (DefaultAzureCredential) through the Aspire Azure AI Inference client integration.
/// No API keys are used or accepted.
/// </summary>
public sealed record LlmEndpointOptions
{
    public const string SectionName = "LlmEndpoint";

    /// <summary>
    /// The Foundry Models endpoint URL (env: FOUNDRY_ENDPOINT).
    /// </summary>
    public string? Endpoint { get; init; }

    /// <summary>
    /// The model deployment name to use (env: FOUNDRY_MODEL_NAME).
    /// </summary>
    public string? ModelName { get; init; }
}
