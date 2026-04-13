using FluentAssertions;
using Marginalia.Api.Controllers;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Marginalia.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Marginalia.Tests.Unit.Api.Controllers;

[TestClass]
[TestCategory("Unit")]
public sealed class DocumentsControllerListTests
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
    public async Task List_ReturnsDocumentSummaries_WhenLegacyDocumentsHaveNullCollections()
    {
        var legacyDocument = new Document
        {
            Id = "doc-1",
            UserId = "user-1",
            Filename = "legacy.docx",
            Source = DocumentSource.Local,
            Title = "",
            Status = DocumentStatus.Draft,
            UpdatedAt = DateTimeOffset.UtcNow,
            Paragraphs = null!,
            Suggestions = null!
        };

        _documentRepository
            .GetByUserAsync("user-1", Arg.Any<CancellationToken>())
            .Returns([legacyDocument]);

        var result = await _controller.List(CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<DocumentListResponse>().Subject;
        response.Documents.Should().ContainSingle();
        response.Documents[0].Title.Should().Be("legacy.docx");
        response.Documents[0].SuggestionCount.Should().Be(0);
        response.Documents[0].ParagraphCount.Should().Be(0);
        response.Documents[0].Status.Should().Be(DocumentStatus.Draft);
    }
}