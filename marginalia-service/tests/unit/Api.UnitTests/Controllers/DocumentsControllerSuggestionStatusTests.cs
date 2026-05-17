using FluentAssertions;
using Marginalia.Api.Controllers;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Marginalia.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Marginalia.Api.UnitTests.Controllers;

[TestClass]
[TestCategory("Unit")]
public sealed class DocumentsControllerSuggestionStatusTests
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
    public async Task UpdateSuggestion_WhenAccepting_RejectsAllOtherSuggestionsForSameParagraph()
    {
        var document = CreateDocumentWithSuggestions([
            CreateSuggestion("s-1", "p-1", SuggestionStatus.Pending),
            CreateSuggestion("s-2", "p-1", SuggestionStatus.Accepted),
            CreateSuggestion("s-3", "p-1", SuggestionStatus.Modified),
            CreateSuggestion("s-4", "p-1", SuggestionStatus.Rejected),
            CreateSuggestion("s-5", "p-2", SuggestionStatus.Pending)
        ]);

        _documentRepository
            .GetByIdAsync("user-1", "doc-1", Arg.Any<CancellationToken>())
            .Returns(document);

        Document? savedDocument = null;
        _documentRepository
            .When(repo => repo.SaveAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => savedDocument = callInfo.Arg<Document>());

        var request = new UpdateSuggestionRequest
        {
            Status = SuggestionStatus.Accepted
        };

        var result = await _controller.UpdateSuggestion("doc-1", "s-1", request, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        savedDocument.Should().NotBeNull();

        var byId = savedDocument!.Suggestions.ToDictionary(s => s.Id, s => s.Status);
        byId["s-1"].Should().Be(SuggestionStatus.Accepted);
        byId["s-2"].Should().Be(SuggestionStatus.Rejected);
        byId["s-3"].Should().Be(SuggestionStatus.Rejected);
        byId["s-4"].Should().Be(SuggestionStatus.Rejected);
        byId["s-5"].Should().Be(SuggestionStatus.Pending);
    }

    [TestMethod]
    public async Task AnalyzeParagraph_WhenAcceptedSuggestionsExist_ResetsThemToPendingAndAppendsNewSuggestions()
    {
        var existingAccepted = CreateSuggestion("s-accepted", "p-1", SuggestionStatus.Accepted);
        var existingModified = CreateSuggestion("s-modified", "p-1", SuggestionStatus.Modified);
        var existingOtherParagraph = CreateSuggestion("s-other", "p-2", SuggestionStatus.Accepted);

        var document = new Document
        {
            Id = "doc-1",
            UserId = "user-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Paragraphs =
            [
                new Paragraph { Id = "p-1", Text = "First paragraph." },
                new Paragraph { Id = "p-2", Text = "Second paragraph." }
            ],
            Suggestions = [existingAccepted, existingModified, existingOtherParagraph]
        };

        var newSuggestion = CreateSuggestion("s-new", "p-1", SuggestionStatus.Pending);

        _documentRepository
            .GetByIdAsync("user-1", "doc-1", Arg.Any<CancellationToken>())
            .Returns(document);

        _suggestionService
            .AnalyzeParagraphAsync(
                "doc-1",
                Arg.Any<Paragraph>(),
                Arg.Any<IReadOnlyList<Paragraph>>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns([newSuggestion]);

        Document? savedDocument = null;
        _documentRepository
            .When(repo => repo.SaveAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => savedDocument = callInfo.Arg<Document>());

        var result = await _controller.AnalyzeParagraph("doc-1", "p-1", request: null, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        savedDocument.Should().NotBeNull();

        var byId = savedDocument!.Suggestions.ToDictionary(s => s.Id);
        byId["s-accepted"].Status.Should().Be(SuggestionStatus.Pending);
        byId["s-modified"].Status.Should().Be(SuggestionStatus.Pending);
        byId["s-other"].Status.Should().Be(SuggestionStatus.Accepted);
        byId["s-new"].Status.Should().Be(SuggestionStatus.Pending);
    }

    private static Document CreateDocumentWithSuggestions(IReadOnlyList<Suggestion> suggestions) => new()
    {
        Id = "doc-1",
        UserId = "user-1",
        Filename = "test.docx",
        Source = DocumentSource.Local,
        Paragraphs =
        [
            new Paragraph { Id = "p-1", Text = "First paragraph." },
            new Paragraph { Id = "p-2", Text = "Second paragraph." }
        ],
        Suggestions = suggestions
    };

    private static Suggestion CreateSuggestion(string id, string paragraphId, SuggestionStatus status) => new()
    {
        Id = id,
        UserId = "user-1",
        DocumentId = "doc-1",
        ParagraphId = paragraphId,
        Rationale = $"Rationale for {id}",
        ProposedChange = $"Change for {id}",
        Status = status
    };
}
