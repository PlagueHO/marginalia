var builder = DistributedApplication.CreateBuilder(args);

var foundry = builder.AddAzureAIFoundry("ai-foundry");

var reviewerDeployment = foundry.AddDeployment(
    "reviewer",
    builder.Configuration["MicrosoftFoundry:modelName"] ?? "gpt-5.3-chat",
    builder.Configuration["MicrosoftFoundry:modelVersion"] ?? "2026-03-03",
    "OpenAI")
    .WithProperties(deployment =>
    {
        deployment.SkuName = "GlobalStandard";
        deployment.SkuCapacity = 50;
    });

#pragma warning disable ASPIRECOSMOSDB001
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsPreviewEmulator(emulator =>
    {
        emulator.WithDataExplorer();
    });
#pragma warning restore ASPIRECOSMOSDB001

var cosmosDb = cosmos.AddCosmosDatabase("marginalia");
var documentsContainer = cosmosDb.AddContainer("documents", "/userId");
var sessionsContainer = cosmosDb.AddContainer("sessions", "/userId");
var suggestionsContainer = cosmosDb.AddContainer("suggestions", "/userId");

var apiService = builder.AddProject<Projects.Marginalia_Api>("api")
    .WithReference(foundry)
    .WithReference(reviewerDeployment)
    .WithReference(cosmos)
    .WithReference(documentsContainer)
    .WithReference(sessionsContainer)
    .WithReference(suggestionsContainer)
    .WaitFor(reviewerDeployment)
    .WaitFor(cosmos)
    .WithEnvironment("AZURE_TENANT_ID", builder.Configuration["Azure:TenantId"] ?? "");

builder.AddViteApp("frontend", "../../../../marginalia-app", "dev")
    .WithPnpm()
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
