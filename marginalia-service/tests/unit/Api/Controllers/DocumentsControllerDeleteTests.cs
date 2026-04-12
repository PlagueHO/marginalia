using FluentAssertions;
using Marginalia.Api.Controllers;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Marginalia.Infrastructure.Services;

namespace Marginalia.Tests.Unit.Api.Controllers;

[TestClass]
[TestCategory("Unit")]
public sealed class DocumentsControllerDeleteTests
{
    private IDocumentRepository _documentRepository = null!;
    private ISessionRepository _sessionRepository = null!;
    private ISuggestionService _suggestionService = null!;
    private IWordDocumentService _wordDocumentService = null!;
    private ILogger<DocumentsController> _logger = null!;
    private SuggestionMergeService _suggestionMergeService = null!;
    private DocumentsController _controller = null!;

    private static Document CreateDocument(string id = "doc-1", string userId = "user-1") => new()
    {
        Id = id,
        UserId = userId,
        Filename = "test.docx",
        Source = DocumentSource.Local,
        Paragraphs = [new Paragraph { Id = "p1", Text = "Test content for deletion." }]
    };

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
    public async Task Delete_ReturnsNoContent_WhenDocumentExists()
    {
        var document = CreateDocument();
        _documentRepository
            .GetByIdAsync("user-1", "doc-1", Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _controller.Delete("doc-1", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        await _documentRepository.Received(1)
            .DeleteAsync("user-1", "doc-1", Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Delete_ReturnsNotFound_WhenDocumentDoesNotExist()
    {
        _documentRepository
            .GetByIdAsync("user-1", "doc-missing", Arg.Any<CancellationToken>())
            .Returns((Document?)null);

        var result = await _controller.Delete("doc-missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        await _documentRepository.DidNotReceive()
            .DeleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Delete_UsesAnonymousUserId_WhenHeaderMissing()
    {
        _controller.ControllerContext.HttpContext.Request.Headers.Remove("X-User-Id");

        var document = CreateDocument(userId: "_anonymous");
        _documentRepository
            .GetByIdAsync("_anonymous", "doc-1", Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _controller.Delete("doc-1", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        await _documentRepository.Received(1)
            .DeleteAsync("_anonymous", "doc-1", Arg.Any<CancellationToken>());
    }
}

