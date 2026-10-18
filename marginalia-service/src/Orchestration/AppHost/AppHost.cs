using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.Expressions;

var builder = DistributedApplication.CreateBuilder(args);

var foundry = builder.AddFoundry("foundry")
    .ConfigureInfrastructure(infra =>
    {
        var account = infra.GetProvisionableResources().OfType<CognitiveServicesAccount>().Single();

        // Azure subdomains are immutable; retain the naming expression used before the Aspire upgrade.
        account.Properties.CustomSubDomainName = BicepFunction.ToLower(BicepFunction.Take(
            BicepFunction.Concat("foundry", BicepFunction.GetUniqueString(BicepFunction.GetResourceGroup().Id)),
            24));
    });
var foundryProject = foundry.AddProject("foundryProject");

var reviewerDeployment = foundryProject.AddModelDeployment(
    "reviewer",
    builder.Configuration["MicrosoftFoundry:modelName"] ?? "gpt-6-luna",
    builder.Configuration["MicrosoftFoundry:modelVersion"] ?? "2026-09-22",
    "OpenAI")
    .WithProperties(deployment =>
    {
        deployment.SkuName = "GlobalStandard";
        deployment.SkuCapacity = 50;
    });

var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsEmulator(emulator =>
    {
        emulator.WithDataExplorer();
    });

var cosmosDb = cosmos.AddCosmosDatabase("marginalia");
var documentsContainer = cosmosDb.AddContainer("documents", "/userId");
var sessionsContainer = cosmosDb.AddContainer("sessions", "/userId");

var apiService = builder.AddProject<Projects.Marginalia_Api>("api")
    .WithReference(foundryProject)
    .WithReference(reviewerDeployment)
    .WithReference(cosmos)
    .WithReference(documentsContainer)
    .WithReference(sessionsContainer)
    .WaitFor(reviewerDeployment)
    .WaitFor(cosmos)
    .WithEnvironment("AZURE_TENANT_ID", builder.Configuration["Azure:TenantId"] ?? "");

builder.AddViteApp("frontend", "../../../../marginalia-app", "dev")
    .WithPnpm()
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
