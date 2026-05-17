using FluentAssertions;
using Marginalia.Api.Controllers;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Marginalia.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Text.Json;

namespace Marginalia.Api.UnitTests.Controllers;

[TestClass]
[TestCategory("Unit")]
public sealed class DocumentsControllerContentFilterTests
{
    private IDocumentRepository _documentRepository = null!;
    private ISessionRepository _sessionRepository = null!;
    private ISuggestionService _suggestionService = null!;
    private IWordDocumentService _wordDocumentService = null!;
    private ILogger<DocumentsController> _logger = null!;
    private SuggestionMergeService _suggestionMergeService = null!;
    private DocumentsController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _documentRepository = Substitute.For<IDocumentRepository>();
        _sessionRepository = Substitute.For<ISessionRepository>();
        _suggestionService = Substitute.For<ISuggestionService>();
        _wordDocumentService = Substitute.For<IWordDocumentService>();
        _logger = Substitute.For<ILogger<DocumentsController>>();
        _suggestionMergeService = new SuggestionMergeService();

        _controller = new DocumentsController(
            _documentRepository,
            _sessionRepository,
            _suggestionService,
            _wordDocumentService,
            _suggestionMergeService,
            _logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-User-Id"] = "user-1";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [TestMethod]
    public async Task Analyze_ReturnsUnprocessableEntity_WhenContentFilterTriggered()
    {
        var document = new Document
        {
            Id = "doc-1",
            UserId = "user-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Title = "Test",
            Status = DocumentStatus.Draft,
            UpdatedAt = DateTimeOffset.UtcNow,
            Paragraphs =
            [
                new Paragraph { Id = "p1", Text = "Some text" }
            ],
            Suggestions = []
        };

        _documentRepository
            .GetByIdAsync("user-1", "doc-1", Arg.Any<CancellationToken>())
            .Returns(document);

        var filterResults = new List<ContentFilterResult>
        {
            new() { Category = "hate", Filtered = true, Severity = "medium" },
            new() { Category = "sexual", Filtered = false, Severity = "low" }
        };

        _suggestionService
            .AnalyzeAsync("doc-1", Arg.Any<IReadOnlyList<Paragraph>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ContentFilterException("Content filtered", filterResults));

        var result = await _controller.Analyze("doc-1", null, CancellationToken.None);

        var unprocessableResult = result.Result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        unprocessableResult.StatusCode.Should().Be(422);

        var json = JsonSerializer.Serialize(unprocessableResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("error").GetString().Should().NotBeNullOrEmpty();
        root.GetProperty("code").GetString().Should().Be("content_filter");
        var categories = root.GetProperty("categories").EnumerateArray().ToList();
        categories.Should().HaveCount(1, "only filtered=true entries should be included");
        categories[0].GetProperty("category").GetString().Should().Be("hate");
        categories[0].GetProperty("severity").GetString().Should().Be("medium");
    }

    [TestMethod]
    public async Task AnalyzeParagraph_ReturnsUnprocessableEntity_WhenContentFilterTriggered()
    {
        var document = new Document
        {
            Id = "doc-1",
            UserId = "user-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Title = "Test",
            Status = DocumentStatus.Draft,
            UpdatedAt = DateTimeOffset.UtcNow,
            Paragraphs =
            [
                new Paragraph { Id = "p1", Text = "Some text" },
                new Paragraph { Id = "p2", Text = "More text" }
            ],
            Suggestions = []
        };

        _documentRepository
            .GetByIdAsync("user-1", "doc-1", Arg.Any<CancellationToken>())
            .Returns(document);

        var filterResults = new List<ContentFilterResult>
        {
            new() { Category = "violence", Filtered = true, Severity = "high" }
        };

        _suggestionService
            .AnalyzeParagraphAsync("doc-1", Arg.Any<Paragraph>(), Arg.Any<IReadOnlyList<Paragraph>>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ContentFilterException("Content filtered", filterResults));

        var result = await _controller.AnalyzeParagraph("doc-1", "p1", null, CancellationToken.None);

        var unprocessableResult = result.Result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        unprocessableResult.StatusCode.Should().Be(422);

        var json = JsonSerializer.Serialize(unprocessableResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("error").GetString().Should().NotBeNullOrEmpty();
        root.GetProperty("code").GetString().Should().Be("content_filter");
        var categories = root.GetProperty("categories").EnumerateArray().ToList();
        categories.Should().HaveCount(1, "only filtered=true entries should be included");
        categories[0].GetProperty("category").GetString().Should().Be("violence");
        categories[0].GetProperty("severity").GetString().Should().Be("high");
    }
}
