using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Azure.Identity;
using Marginalia.Domain.Configuration;
using Marginalia.Domain.Models;
using Marginalia.Infrastructure.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Marginalia.Ai.EvaluationTests;

internal static class FoundrySuggestionEvaluationSupport
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static FoundrySuggestionScenarioSet LoadScenarioSet()
    {
        var scenarioFilePath = Path.Combine(AppContext.BaseDirectory, "Scenarios", "foundry-suggestion-scenarios.json");
        var content = File.ReadAllText(scenarioFilePath);
        return JsonSerializer.Deserialize<FoundrySuggestionScenarioSet>(content, s_jsonSerializerOptions)
               ?? throw new InvalidOperationException($"Failed to load AI evaluation scenarios from '{scenarioFilePath}'.");
    }

    public static ReportingConfiguration CreateReportingConfiguration(FoundrySuggestionEvaluationEnvironment environment)
    {
        Directory.CreateDirectory(environment.StorageRootPath);

        var judgeHttpClient = CreateFoundryHttpClient();
        var judgeChatClient = new FoundryOpenAiChatClient(
            judgeHttpClient,
            new DefaultAzureCredential(),
            environment.FoundryProjectEndpoint,
            environment.ModelName);

        return DiskBasedReportingConfiguration.Create(
            storageRootPath: environment.StorageRootPath,
            evaluators:
            [
                new ParagraphMappingEvaluator(),
                new UniqueParagraphTargetEvaluator(),
                new SuggestionFieldsEvaluator(),
                new ExpectedCoverageEvaluator(),
                new MeaningfulRewriteEvaluator(),
                new RelevanceEvaluator(),
                new CoherenceEvaluator(),
            ],
            chatConfiguration: new ChatConfiguration(judgeChatClient),
            enableResponseCaching: environment.EnableResponseCaching,
            executionName: environment.ExecutionName);
    }

    public static FoundrySuggestionService CreateSuggestionService(FoundrySuggestionEvaluationEnvironment environment)
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient.GetService<ChatClientMetadata>().Returns(
            new ChatClientMetadata(
                providerName: "FoundrySuggestionEval",
                providerUri: environment.FoundryProjectEndpoint,
                defaultModelId: environment.ModelName));

        return new FoundrySuggestionService(
            chatClient,
            NullLogger<FoundrySuggestionService>.Instance,
            new FixedOptionsMonitor<LlmEndpointOptions>(
                new LlmEndpointOptions
                {
                    Endpoint = environment.FoundryProjectEndpoint.ToString(),
                    ModelName = environment.ModelName
                }),
            new SingleClientHttpClientFactory(CreateFoundryHttpClient()),
            new DefaultAzureCredential());
    }

    public static async Task<IReadOnlyList<Suggestion>> RunDeployedScenarioAsync(
        FoundrySuggestionEvaluationEnvironment environment,
        FoundrySuggestionScenario scenario,
        CancellationToken cancellationToken)
    {
        if (environment.ApiBaseUrl is null)
        {
            throw new InvalidOperationException("A deployed API base URL is required for the deployed evaluation canary.");
        }

        using var client = new HttpClient
        {
            BaseAddress = environment.ApiBaseUrl,
            Timeout = TimeSpan.FromMinutes(5)
        };

        client.DefaultRequestHeaders.Add("X-User-Id", environment.UserId);
        if (!string.IsNullOrWhiteSpace(environment.AccessCode))
        {
            client.DefaultRequestHeaders.Add("X-Access-Code", environment.AccessCode);
        }

        using var pasteResponse = await client.PostAsJsonAsync(
            "/api/documents/paste",
            new PasteDocumentRequest
            {
                Content = string.Join(Environment.NewLine + Environment.NewLine, scenario.Paragraphs.Select(paragraph => paragraph.Text)),
                Filename = $"{scenario.Id}.txt",
                Title = scenario.Description
            },
            cancellationToken);

        pasteResponse.EnsureSuccessStatusCode();
        var uploadResponse = await pasteResponse.Content.ReadFromJsonAsync<UploadDocumentResponse>(cancellationToken)
                             ?? throw new InvalidOperationException("The deployed API did not return a document response for the paste request.");

        var paragraphIdMap = uploadResponse.Document.Paragraphs
            .Zip(
                scenario.Paragraphs,
                static (actualParagraph, scenarioParagraph) => new
                {
                    ActualParagraphId = actualParagraph.Id,
                    ScenarioParagraphId = scenarioParagraph.Id
                })
            .ToDictionary(item => item.ActualParagraphId, item => item.ScenarioParagraphId, StringComparer.Ordinal);

        using var analyzeResponse = await client.PostAsJsonAsync(
            $"/api/documents/{uploadResponse.Document.Id}/analyze",
            new AnalysisRequest
            {
                UserInstructions = scenario.UserGuidance
            },
            cancellationToken);

        analyzeResponse.EnsureSuccessStatusCode();
        var suggestions = await analyzeResponse.Content.ReadFromJsonAsync<IReadOnlyList<Suggestion>>(cancellationToken)
                          ?? [];

        return suggestions
            .Select(suggestion =>
                paragraphIdMap.TryGetValue(suggestion.ParagraphId, out var scenarioParagraphId)
                    ? suggestion with { ParagraphId = scenarioParagraphId }
                    : suggestion)
            .ToList()
            .AsReadOnly();
    }

    public static IReadOnlyList<ChatMessage> BuildEvaluationMessages(FoundrySuggestionScenario scenario)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Review the editorial suggestions produced for the manuscript excerpt below.");
        prompt.AppendLine("The suggestions should be relevant to the text and the guidance, and they should provide coherent replacement paragraphs.");
        prompt.AppendLine();
        prompt.AppendLine($"Scenario: {scenario.Description}");
        if (!string.IsNullOrWhiteSpace(scenario.UserGuidance))
        {
            prompt.AppendLine($"Author guidance: {scenario.UserGuidance}");
        }

        prompt.AppendLine();
        prompt.AppendLine("Paragraphs under review:");
        foreach (var paragraph in scenario.Paragraphs)
        {
            prompt.AppendLine($"[{paragraph.Id}] {paragraph.Text}");
        }

        return
        [
            new ChatMessage(
                ChatRole.System,
                "You are grading the quality of editorial suggestions for a long-form non-fiction manuscript."),
            new ChatMessage(ChatRole.User, prompt.ToString())
        ];
    }

    public static ChatResponse BuildEvaluationResponse(
        FoundrySuggestionScenario scenario,
        IReadOnlyList<Suggestion> suggestions)
    {
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, RenderSuggestions(scenario, suggestions)));
    }

    private static HttpClient CreateFoundryHttpClient() =>
        new()
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

    private static string RenderSuggestions(
        FoundrySuggestionScenario scenario,
        IReadOnlyList<Suggestion> suggestions)
    {
        if (suggestions.Count == 0)
        {
            return "No suggestions were returned for this scenario.";
        }

        var paragraphLookup = scenario.GetParagraphTextById();
        var builder = new StringBuilder();
        builder.AppendLine("Editorial suggestions:");

        foreach (var suggestion in suggestions)
        {
            paragraphLookup.TryGetValue(suggestion.ParagraphId, out var originalParagraphText);

            builder.AppendLine();
            builder.AppendLine($"ParagraphId: {suggestion.ParagraphId}");
            if (!string.IsNullOrWhiteSpace(originalParagraphText))
            {
                builder.AppendLine($"Original: {originalParagraphText}");
            }
            builder.AppendLine($"Rationale: {suggestion.Rationale}");
            builder.AppendLine($"ProposedChange: {suggestion.ProposedChange}");
        }

        return builder.ToString();
    }
}

internal sealed class FixedOptionsMonitor<TOptions>(TOptions currentValue) : IOptionsMonitor<TOptions>
    where TOptions : class
{
    public TOptions CurrentValue => currentValue;

    public TOptions Get(string? name) => currentValue;

    public IDisposable OnChange(Action<TOptions, string?> listener) => EmptyDisposable.Instance;

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}

internal sealed class SingleClientHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => httpClient;
}
