# .NET Backend Patterns

> Confidence: high
> Source: Gilfoyle — backend implementation for Marginalia

## Pattern: OpenXml Type Alias

When using `DocumentFormat.OpenXml` alongside domain models that have a `Document` class, use a type alias to disambiguate:

```csharp
using DomainDocument = Marginalia.Domain.Models.Document;
```

This avoids `CS0104` ambiguous reference errors between `Marginalia.Domain.Models.Document` and `DocumentFormat.OpenXml.Wordprocessing.Document`.

## Pattern: Foundry API Integration

Use `AddHttpClient<ISuggestionService, FoundrySuggestionService>()` for typed HttpClient injection. When running under Aspire, `IChatClient` is injected via DI and used instead of raw HTTP. The HttpClient path remains as fallback for standalone BYO model usage. Internal DTOs prefixed `FoundryApi*` to avoid namespace conflicts with `Microsoft.Extensions.AI`.

```csharp
[JsonSerializable(typeof(FoundryApiRequest))]
[JsonSerializable(typeof(FoundryApiResponse))]
internal sealed partial class FoundrySerializerContext : JsonSerializerContext;
```

## Pattern: Aspire Integration

ServiceDefaults + AppHost follow the prompt-babbler pattern. Conditional IChatClient registration:

```csharp
var aiConnectionString = builder.Configuration.GetConnectionString("ai-foundry");
if (!string.IsNullOrWhiteSpace(aiConnectionString))
{
    builder.AddAzureOpenAIClient("ai-foundry");
    builder.Services.AddSingleton<IChatClient>(sp =>
    {
        var openAiClient = sp.GetRequiredService<OpenAIClient>();
        return openAiClient.GetChatClient("chat").AsIChatClient();
    });
}
```

`AddServiceDefaults()` goes first after `CreateBuilder`. `MapDefaultEndpoints()` goes before `app.Run()`.

## Pattern: Runtime Config Update

Use `IOptionsMonitor<T>` (not `IOptions<T>`) when config must be updatable at runtime. Add an `InMemoryCollection` config source so `IConfiguration[key] = value` works for runtime updates.

```csharp
builder.Configuration.AddInMemoryCollection();
builder.Services.Configure<LlmEndpointOptions>(
    builder.Configuration.GetSection(LlmEndpointOptions.SectionName));
```

## When to Apply

- Any .NET service in this repo that uses OpenXml
- Any service calling Foundry/OpenAI chat completion endpoints
- Any config that needs BYO model runtime updates
