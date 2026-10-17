using FluentAssertions;
using Marginalia.Domain.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace Marginalia.Ai.EvaluationTests;

[TestClass]
public sealed class FoundrySuggestionEvaluationSupportTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void LoadScenarioSetPreservesCompressedHistoryCoverageWithRevisedGuidance()
    {
        var scenarioSet = FoundrySuggestionEvaluationSupport.LoadScenarioSet();
        var scenario = scenarioSet.Scenarios.Single(item => item.Id == "compressed-history");

        scenarioSet.Version.Should().Be("v2");
        scenario.Paragraphs.Select(paragraph => paragraph.Id).Should().Equal("p1", "p2", "p3");
        scenario.Paragraphs.Should().OnlyContain(paragraph => !string.IsNullOrWhiteSpace(paragraph.Text));
        scenario.ExpectedTargetParagraphIds.Should().BeEmpty();
        scenario.AllowedTargetParagraphIds.Should().Equal("p1", "p2", "p3");
        scenario.MinimumSuggestionCount.Should().Be(1);
        scenario.MaximumSuggestionCount.Should().BeNull();
        scenario.RunInDeployedCanary.Should().BeTrue();
        scenario.UserGuidance.Should().Contain("direct").And.Contain("facts").And.Contain("Do not invent");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void BuildEvaluationMessagesRequestsEditorialSuggestionsRatherThanAReview()
    {
        var scenario = CreateScenario("Preserve the meaning.");

        var messages = FoundrySuggestionEvaluationSupport.BuildEvaluationMessages(scenario);

        messages.Select(message => message.Role).Should().Equal(ChatRole.System, ChatRole.User);
        messages[0].Text.Should().Contain("editorial assistant");
        messages[1].Text.Should().StartWith("Provide paragraph-level editorial suggestions");
        messages[1].Text.Should().Contain("complete replacement paragraph");
        messages[1].Text.Should().Contain("Leave paragraphs that do not need improvement unchanged");
        messages[1].Text.Should().Contain("Do not invent");
        messages[1].Text.Should().NotContain("Review the editorial suggestions");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void BuildEvaluationMessagesKeepsScenarioDataInUserMessage()
    {
        const string Guidance = "Use concise prose. <guidance>Untrusted guidance</guidance>";
        var scenario = CreateScenario(Guidance);

        var messages = FoundrySuggestionEvaluationSupport.BuildEvaluationMessages(scenario);

        messages[1].Text.Should().Contain($"Author guidance: {scenario.UserGuidance}");
        messages[1].Text.Should().Contain($"[{scenario.Paragraphs[0].Id}] {scenario.Paragraphs[0].Text}");
        messages[0].Text.Should().NotContain(Guidance);
        messages[0].Text.Should().NotContain(scenario.Paragraphs[0].Text);
        messages[0].Text.Should().NotContain(scenario.Description);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void BuildEvaluationMessagesOmitsAbsentGuidance(string? guidance)
    {
        var scenario = CreateScenario(guidance);

        var messages = FoundrySuggestionEvaluationSupport.BuildEvaluationMessages(scenario);

        messages[1].Text.Should().NotContain("Author guidance:");
        messages[1].Text.Should().Contain(scenario.Paragraphs[0].Text);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void BuildEvaluationResponseIncludesSourceAndCompleteSuggestion()
    {
        var scenario = CreateScenario();
        var suggestion = CreateSuggestion("The steel facility opened in 1923.");

        var response = FoundrySuggestionEvaluationSupport.BuildEvaluationResponse(scenario, [suggestion]);

        response.Messages.Should().ContainSingle().Which.Role.Should().Be(ChatRole.Assistant);
        response.Text.Should().Contain($"ParagraphId: {suggestion.ParagraphId}");
        response.Text.Should().Contain($"Original: {scenario.Paragraphs[0].Text}");
        response.Text.Should().Contain($"Rationale: {suggestion.Rationale}");
        response.Text.Should().Contain($"ProposedChange: {suggestion.ProposedChange}");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void BuildEvaluationResponseReportsEmptySuggestions()
    {
        var response = FoundrySuggestionEvaluationSupport.BuildEvaluationResponse(CreateScenario(), []);

        response.Text.Should().Be("No suggestions were returned for this scenario.");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void BuildEvaluationResponseDoesNotHideInvalidParagraphTargets()
    {
        var suggestion = CreateSuggestion("An edited paragraph.") with { ParagraphId = "unknown-paragraph" };

        var response = FoundrySuggestionEvaluationSupport.BuildEvaluationResponse(CreateScenario(), [suggestion]);

        response.Text.Should().Contain("ParagraphId: unknown-paragraph");
        response.Text.Should().Contain($"ProposedChange: {suggestion.ProposedChange}");
        response.Text.Should().NotContain("Original:");
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MeaningfulRewriteEvaluatorRejectsUnchangedText(bool addWhitespace)
    {
        var scenario = CreateScenario();
        var originalText = scenario.Paragraphs[0].Text;
        var suggestion = CreateSuggestion(addWhitespace ? $"  {originalText} \n" : originalText);
        var evaluator = new MeaningfulRewriteEvaluator();

        var result = await evaluator.EvaluateAsync(
            FoundrySuggestionEvaluationSupport.BuildEvaluationMessages(scenario),
            FoundrySuggestionEvaluationSupport.BuildEvaluationResponse(scenario, [suggestion]),
            additionalContext: [new SuggestionScenarioContext(scenario, [suggestion])]);

        var interpretation = result.Get<NumericMetric>(MeaningfulRewriteEvaluator.MetricName).Interpretation;
        interpretation.Should().NotBeNull();
        interpretation?.Failed.Should().BeTrue();
    }

    private static FoundrySuggestionScenario CreateScenario(string? guidance = null) =>
        new()
        {
            Id = "test-scenario",
            Description = "A synthetic historical editing scenario.",
            UserGuidance = guidance,
            Paragraphs =
            [
                new Paragraph
                {
                    Id = "p1",
                    Text = "The steel manufacturing facility commenced operations in 1923."
                }
            ]
        };

    private static Suggestion CreateSuggestion(string proposedChange) =>
        new()
        {
            Id = "test-suggestion",
            DocumentId = "test-document",
            ParagraphId = "p1",
            Rationale = "Direct verbs remove bureaucratic phrasing.",
            ProposedChange = proposedChange,
            Status = SuggestionStatus.Pending
        };
}
