using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using Marginalia.Api.HealthChecks;
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

// Aspire Azure AI Inference integration — active when running under Aspire AppHost
var aiConnectionString = builder.Configuration.GetConnectionString("ai-foundry");
if (!string.IsNullOrWhiteSpace(aiConnectionString))
{
    builder.AddAzureChatCompletionsClient("ai-foundry")
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
