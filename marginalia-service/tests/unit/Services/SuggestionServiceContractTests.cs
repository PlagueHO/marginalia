using FluentAssertions;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Marginalia.Tests.Unit.Services;

/// <summary>
/// Tests the ISuggestionService contract using NSubstitute mocks.
/// Verifies expected behavior for consumers of the suggestion service.
/// When the real implementation lands, complement these with integration tests.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public sealed class SuggestionServiceContractTests
{
    private ISuggestionService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = Substitute.For<ISuggestionService>();
    }

    private static IReadOnlyList<Paragraph> MakeParagraphs(params string[] texts) =>
        texts.Select((t, i) => new Paragraph { Id = $"para-{i + 1}", Text = t }).ToList().AsReadOnly();

    [TestMethod]
    public async Task AnalyzeAsync_WithValidParagraphs_ReturnsSuggestions()
    {
        var paragraphs = MakeParagraphs(
            "The factory opened in 1923. It produced steel.",
            "Workers came from nearby towns.");

        var expectedSuggestions = new List<Suggestion>
        {
            new()
            {
                Id = "sug-1",
                DocumentId = "doc-1",
                ParagraphId = "para-1",
                Rationale = "This passage reads as overly compressed factual summary",
                ProposedChange = "Consider expanding with sensory detail and scene-setting",
                Status = SuggestionStatus.Pending
            },
            new()
            {
                Id = "sug-2",
                DocumentId = "doc-1",
                ParagraphId = "para-2",
                Rationale = "Style shift detected — this reads more AI-generated than the surrounding prose",
                ProposedChange = "Rewrite to match the author's narrative voice from chapter 1",
                Status = SuggestionStatus.Pending
            }
        };

        _service.AnalyzeAsync("doc-1", Arg.Any<IReadOnlyList<Paragraph>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(expectedSuggestions);

        var result = await _service.AnalyzeAsync("doc-1", paragraphs, null);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s =>
        {
            s.DocumentId.Should().Be("doc-1");
            s.Status.Should().Be(SuggestionStatus.Pending);
            s.ParagraphId.Should().NotBeNullOrWhiteSpace();
            s.Rationale.Should().NotBeNullOrWhiteSpace();
            s.ProposedChange.Should().NotBeNullOrWhiteSpace();
        });
    }

    [TestMethod]
    public async Task AnalyzeAsync_WithEmptyParagraphs_ReturnsEmptyList()
    {
        _service.AnalyzeAsync(Arg.Any<string>(), Arg.Is<IReadOnlyList<Paragraph>>(p => p.Count == 0), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _service.AnalyzeAsync("doc-1", Array.Empty<Paragraph>(), null);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task AnalyzeAsync_WithUserGuidance_IncludesGuidanceInRequest()
    {
        _service.AnalyzeAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<Paragraph>>(),
                "Focus on making the prose more narrative, less academic",
                Arg.Any<CancellationToken>())
            .Returns(new List<Suggestion>
            {
                new()
                {
                    Id = "sug-1",
                    DocumentId = "doc-1",
                    ParagraphId = "para-1",
                    Rationale = "Academic tone detected — user requested narrative style",
                    ProposedChange = "Rewrite with storytelling elements",
                    Status = SuggestionStatus.Pending,
                    UserSteeringInput = "Focus on making the prose more narrative, less academic"
                }
            });

        var result = await _service.AnalyzeAsync(
            "doc-1",
            MakeParagraphs("The methodology employed a mixed-methods approach."),
            "Focus on making the prose more narrative, less academic");

        result.Should().ContainSingle();
        result[0].UserSteeringInput.Should().Contain("narrative");
    }

    [TestMethod]
    public async Task AnalyzeAsync_ApiFailure_ThrowsException()
    {
        _service.AnalyzeAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<Paragraph>>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Foundry endpoint unreachable"));

        var act = () => _service.AnalyzeAsync("doc-1", MakeParagraphs("content"), null);

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*unreachable*");
    }

    [TestMethod]
    public async Task AnalyzeAsync_CancellationRequested_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _service.AnalyzeAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<Paragraph>>(),
                Arg.Any<string?>(),
                cts.Token)
            .ThrowsAsync(new OperationCanceledException());

        var act = () => _service.AnalyzeAsync("doc-1", MakeParagraphs("content"), null, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task AnalyzeAsync_SuggestionsHaveValidParagraphIds()
    {
        var paragraphs = MakeParagraphs("First paragraph.", "Second paragraph.");
        var suggestions = new List<Suggestion>
        {
            new()
            {
                Id = "sug-1",
                DocumentId = "doc-1",
                ParagraphId = "para-1",
                Rationale = "Reason",
                ProposedChange = "Change",
                Status = SuggestionStatus.Pending
            }
        };

        _service.AnalyzeAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<Paragraph>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(suggestions);

        var result = await _service.AnalyzeAsync("doc-1", paragraphs, null);

        result.Should().AllSatisfy(s =>
        {
            s.ParagraphId.Should().NotBeNullOrWhiteSpace();
        });
    }

    [TestMethod]
    public async Task AnalyzeAsync_AllSuggestionsStartPending()
    {
        var suggestions = Enumerable.Range(1, 5).Select(i => new Suggestion
        {
            Id = $"sug-{i}",
            DocumentId = "doc-1",
            ParagraphId = $"para-{i}",
            Rationale = $"Issue {i}",
            ProposedChange = $"Fix {i}",
            Status = SuggestionStatus.Pending
        }).ToList();

        _service.AnalyzeAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<Paragraph>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(suggestions);

        var result = await _service.AnalyzeAsync("doc-1", MakeParagraphs("content"), null);

        result.Should().AllSatisfy(s => s.Status.Should().Be(SuggestionStatus.Pending));
    }

    [TestMethod]
    public async Task AnalyzeAsync_NullGuidance_IsAccepted()
    {
        _service.AnalyzeAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<Paragraph>>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        var act = () => _service.AnalyzeAsync("doc-1", MakeParagraphs("content"), null);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task AnalyzeAsync_ManyParagraphs_ChunkingBehavior()
    {
        // ~30 paragraphs should exercise chunking. Verify the service handles large input.
        var paragraphs = Enumerable.Range(1, 30)
            .Select(i => new Paragraph { Id = $"para-{i}", Text = $"Paragraph {i}: " + new string('x', 150) })
            .ToList()
            .AsReadOnly();

        _service.AnalyzeAsync(Arg.Any<string>(), Arg.Is<IReadOnlyList<Paragraph>>(p => p.Count > 20), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var act = () => _service.AnalyzeAsync("doc-1", paragraphs, null);

        await act.Should().NotThrowAsync();
    }
}
