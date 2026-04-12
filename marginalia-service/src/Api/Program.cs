using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using Marginalia.Api.Authentication;
using Marginalia.Api.HealthChecks;
using Marginalia.Api.Middleware;
using Marginalia.Domain.Configuration;
using Marginalia.Domain.Interfaces;
using Marginalia.Infrastructure.Repositories;
using Marginalia.Infrastructure.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// LLM configuration — environment variables override appsettings
builder.Configuration.AddInMemoryCollection();
if (Environment.GetEnvironmentVariable("FOUNDRY_ENDPOINT") is { Length: > 0 } endpoint)
{
    builder.Configuration[$"{LlmEndpointOptions.SectionName}:Endpoint"] = endpoint;
}

if (Environment.GetEnvironmentVariable("FOUNDRY_MODEL_NAME") is { Length: > 0 } modelName)
{
    builder.Configuration[$"{LlmEndpointOptions.SectionName}:ModelName"] = modelName;
}

builder.Services.Configure<LlmEndpointOptions>(
    builder.Configuration.GetSection(LlmEndpointOptions.SectionName));

// Access control — optional access code for single-user mode
if (Environment.GetEnvironmentVariable("ACCESS_CODE") is { Length: > 0 } accessCode)
{
    builder.Configuration[$"{AccessControlOptions.SectionName}:AccessCode"] = accessCode;
}

builder.Services.Configure<AccessControlOptions>(
    builder.Configuration.GetSection(AccessControlOptions.SectionName));

// Cosmos DB client — use ManagedIdentityCredential directly in deployed environments.
// DefaultAzureCredential permanently caches credential unavailability per the Azure Identity
// SDK behavior. In ACA cold starts (scale from 0), the identity sidecar may not be ready on
// the first credential probe, causing DAC to mark ManagedIdentityCredential as permanently
// unavailable and fall through to VS/VSCode credentials which don't exist in a Linux container.
// See: https://learn.microsoft.com/azure/container-apps/managed-identity?tabs=portal,dotnet
builder.AddAzureCosmosClient("cosmos",
    configureSettings: settings =>
    {
        // In non-development environments (ACA), use ManagedIdentityCredential directly
        // with system-assigned identity. DefaultAzureCredential is used in development
        // where it picks up local credentials (Azure CLI, VS, etc.).
        if (!builder.Environment.IsDevelopment())
        {
            settings.Credential = new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
        }
    },
    configureClientOptions: options =>
    {
        options.UseSystemTextJsonSerializerWithOptions = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
    });

// DI registrations — use Cosmos repositories
builder.Services.AddSingleton<IDocumentRepository>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var logger = sp.GetRequiredService<ILogger<CosmosDocumentRepository>>();
    return new CosmosDocumentRepository(cosmosClient, logger);
});
builder.Services.AddSingleton<ISessionRepository>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var logger = sp.GetRequiredService<ILogger<CosmosSessionRepository>>();
    return new CosmosSessionRepository(cosmosClient, logger);
});
builder.Services.AddSingleton<IWordDocumentService, WordDocumentService>();
builder.Services.AddSingleton<SuggestionMergeService>();
builder.Services.AddHttpClient();

// Named HttpClient for LLM fallback — the standard resilience handler's 30s
// total timeout is too short for large-document chat completions. Configure
// a dedicated handler with extended timeouts for this client.
builder.Services.AddHttpClient("foundry-llm", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
}).ConfigureAdditionalHttpMessageHandlers((handlers, _) =>
{
    // Remove the default resilience handlers added by ConfigureHttpClientDefaults
    // so we can apply our own timeout configuration below.
    handlers.Clear();
}).AddStandardResilienceHandler(options =>
{
    options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
    options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(4);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(10);
    options.Retry.MaxRetryAttempts = 2;
});

// Aspire Azure AI Inference integration — active when running under Aspire AppHost.
// In non-development environments (ACA), use ManagedIdentityCredential directly
// to avoid the same DefaultAzureCredential cold-start caching issue as Cosmos DB.
var aiConnectionString = builder.Configuration.GetConnectionString("ai-foundry");
if (!string.IsNullOrWhiteSpace(aiConnectionString))
{
    var aiTokenCredential = new AiFoundryTokenCredential(
        builder.Environment.IsDevelopment()
            ? new DefaultAzureCredential()
            : new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned));

    builder.Services.AddSingleton<TokenCredential>(aiTokenCredential);

    builder.AddAzureChatCompletionsClient("ai-foundry",
        configureSettings: settings =>
        {
            settings.TokenCredential = aiTokenCredential;
        })
        .AddChatClient("reviewer");
    builder.Services.AddSingleton<ISuggestionService, FoundrySuggestionService>();
}
else
{
    builder.Services.AddSingleton<ISuggestionService, NoOpSuggestionService>();
}

// Health checks for dependency monitoring
builder.Services.AddHealthChecks()
    .AddCheck<CosmosDbHealthCheck>("cosmosdb", tags: ["ready"])
    .AddCheck<AiFoundryHealthCheck>("ai-foundry", tags: ["ready"]);

// Managed identity health check only runs in deployed environments
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHealthChecks()
        .AddCheck<ManagedIdentityHealthCheck>("managed-identity", tags: ["ready"]);
}

// Controllers + JSON config
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// OpenAPI / Swagger
builder.Services.AddOpenApi();

// CORS — allow any localhost port in dev; use CORS:AllowedOrigins in production
var corsAllowedOrigins = builder.Configuration["CORS:AllowedOrigins"] ?? "";

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        static bool IsLocalOrigin(string origin) =>
            Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Host is "localhost" or "127.0.0.1";

        if (string.IsNullOrEmpty(corsAllowedOrigins))
        {
            policy.SetIsOriginAllowed(IsLocalOrigin)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            var allowedOrigins = corsAllowedOrigins
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            policy.SetIsOriginAllowed(origin =>
                    IsLocalOrigin(origin) || allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// File upload size limit
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 52_428_800; // 50 MB
});

// Startup diagnostic logging
var startupLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
startupLogger.LogInformation("AI Foundry connection string: {Status}",
    string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("ai-foundry")) ? "(not set)" : "(set)");
startupLogger.LogInformation("FOUNDRY_ENDPOINT: {Status}",
    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FOUNDRY_ENDPOINT")) ? "(not set)" : "(set)");
startupLogger.LogInformation("CORS mode: {CorsMode}",
    string.IsNullOrEmpty(corsAllowedOrigins) ? "local dev (any localhost origin)" : "configured origins");
startupLogger.LogInformation("OTEL exporter endpoint: {Status}",
    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")) ? "(not set)" : "(set)");
startupLogger.LogInformation("Access control: {Status}",
    string.IsNullOrEmpty(builder.Configuration[$"{AccessControlOptions.SectionName}:AccessCode"]) ? "disabled (no access code)" : "enabled (access code set)");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Exception handler must precede UseCors so that when re-dispatching the error
// path the CORS middleware runs and adds Access-Control-Allow-Origin on error responses.
app.UseExceptionHandler("/api/error");
app.UseCors();
app.UseMiddleware<AccessCodeMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapDefaultEndpoints();

// Minimal error endpoint — reached via UseExceptionHandler re-dispatch.
app.Map("/api/error", () =>
    Results.Problem(
        title: "An unexpected error occurred. Please try again.",
        statusCode: StatusCodes.Status500InternalServerError));

app.Run();
