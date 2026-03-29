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

    [TestMethod]
    public async Task AnalyzeAsync_WithValidContent_ReturnsSuggestions()
    {
        var expectedSuggestions = new List<Suggestion>
        {
            new()
            {
                Id = "sug-1",
                DocumentId = "doc-1",
                TextRange = new TextRange { Start = 0, End = 100 },
                Rationale = "This passage reads as overly compressed factual summary",
                ProposedChange = "Consider expanding with sensory detail and scene-setting",
                Status = SuggestionStatus.Pending
            },
            new()
            {
                Id = "sug-2",
                DocumentId = "doc-1",
                TextRange = new TextRange { Start = 200, End = 350 },
                Rationale = "Style shift detected — this reads more AI-generated than the surrounding prose",
                ProposedChange = "Rewrite to match the author's narrative voice from chapter 1",
                Status = SuggestionStatus.Pending
            }
        };

        _service.AnalyzeAsync("doc-1", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(expectedSuggestions);

        var result = await _service.AnalyzeAsync(
            "doc-1",
            "The factory opened in 1923. It produced steel. Workers came from nearby towns.",
            null);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s =>
        {
            s.DocumentId.Should().Be("doc-1");
            s.Status.Should().Be(SuggestionStatus.Pending);
            s.TextRange.Start.Should().BeGreaterThanOrEqualTo(0);
            s.TextRange.End.Should().BeGreaterThan(s.TextRange.Start);
            s.Rationale.Should().NotBeNullOrWhiteSpace();
            s.ProposedChange.Should().NotBeNullOrWhiteSpace();
        });
    }

    [TestMethod]
    public async Task AnalyzeAsync_WithEmptyContent_ReturnsEmptyList()
    {
        _service.AnalyzeAsync(Arg.Any<string>(), "", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _service.AnalyzeAsync("doc-1", "", null);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task AnalyzeAsync_WithUserGuidance_IncludesGuidanceInRequest()
    {
        _service.AnalyzeAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                "Focus on making the prose more narrative, less academic",
                Arg.Any<CancellationToken>())
            .Returns(new List<Suggestion>
            {
                new()
                {
                    Id = "sug-1",
                    DocumentId = "doc-1",
                    TextRange = new TextRange { Start = 0, End = 50 },
                    Rationale = "Academic tone detected — user requested narrative style",
                    ProposedChange = "Rewrite with storytelling elements",
                    Status = SuggestionStatus.Pending,
                    UserSteeringInput = "Focus on making the prose more narrative, less academic"
                }
            });

        var result = await _service.AnalyzeAsync(
            "doc-1",
            "The methodology employed a mixed-methods approach.",
            "Focus on making the prose more narrative, less academic");

        result.Should().ContainSingle();
        result[0].UserSteeringInput.Should().Contain("narrative");
    }

    [TestMethod]
    public async Task AnalyzeAsync_ApiFailure_ThrowsException()
    {
        _service.AnalyzeAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Foundry endpoint unreachable"));

        var act = () => _service.AnalyzeAsync("doc-1", "content", null);

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
                Arg.Any<string>(),
                Arg.Any<string?>(),
                cts.Token)
            .ThrowsAsync(new OperationCanceledException());

        var act = () => _service.AnalyzeAsync("doc-1", "content", null, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task AnalyzeAsync_SuggestionsHaveValidTextRanges()
    {
        var suggestions = new List<Suggestion>
        {
            new()
            {
                Id = "sug-1",
                DocumentId = "doc-1",
                TextRange = new TextRange { Start = 10, End = 150 },
                Rationale = "Reason",
                ProposedChange = "Change",
                Status = SuggestionStatus.Pending
            }
        };

        _service.AnalyzeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(suggestions);

        var result = await _service.AnalyzeAsync("doc-1", "Long content here...", null);

        result.Should().AllSatisfy(s =>
        {
            s.TextRange.Start.Should().BeGreaterThanOrEqualTo(0);
            s.TextRange.End.Should().BeGreaterThan(s.TextRange.Start);
        });
    }

    [TestMethod]
    public async Task AnalyzeAsync_AllSuggestionsStartPending()
    {
        var suggestions = Enumerable.Range(1, 5).Select(i => new Suggestion
        {
            Id = $"sug-{i}",
            DocumentId = "doc-1",
            TextRange = new TextRange { Start = i * 100, End = i * 100 + 50 },
            Rationale = $"Issue {i}",
            ProposedChange = $"Fix {i}",
            Status = SuggestionStatus.Pending
        }).ToList();

        _service.AnalyzeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(suggestions);

        var result = await _service.AnalyzeAsync("doc-1", "content", null);

        result.Should().AllSatisfy(s => s.Status.Should().Be(SuggestionStatus.Pending));
    }

    [TestMethod]
    public async Task AnalyzeAsync_NullGuidance_IsAccepted()
    {
        _service.AnalyzeAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns([]);

        var act = () => _service.AnalyzeAsync("doc-1", "content", null);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task AnalyzeAsync_LargeContent_ChunkingBehavior()
    {
        // ~3 pages is roughly 4500 characters. Verify the service handles large content.
        var largeContent = string.Join("\n\n", Enumerable.Range(1, 30)
            .Select(i => $"Paragraph {i}: " + new string('x', 150)));

        _service.AnalyzeAsync(Arg.Any<string>(), Arg.Is<string>(c => c.Length > 4000), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var act = () => _service.AnalyzeAsync("doc-1", largeContent, null);

        await act.Should().NotThrowAsync();
    }
}
