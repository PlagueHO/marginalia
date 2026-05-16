using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
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
public sealed class DocumentsControllerImportExportScopeTests
{
    private static readonly JsonSerializerOptions ArchiveSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

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

        CreateController(isMultiUserMode: false);
    }

    [TestMethod]
    public async Task ExportAll_InSingleUserMode_ExportsAllManuscripts()
    {
        _controller.ControllerContext.HttpContext.Request.Headers.Remove("X-User-Id");

        var documents = new List<Document>
        {
            CreateDocument("doc-1", "user-alice"),
            CreateDocument("doc-2", "user-bob")
        };

        _documentRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(documents);

        var result = await _controller.ExportAll(CancellationToken.None);

        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        var exportedDocuments = await ReadArchiveDocumentsAsync(fileResult.FileStream);

        exportedDocuments.Should().HaveCount(2);
        exportedDocuments.Select(d => d.UserId).Should().Contain(["user-alice", "user-bob"]);

        await _documentRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
        await _documentRepository.DidNotReceive().GetByUserAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ExportAll_InMultiUserMode_ExportsOnlyCurrentUserManuscripts()
    {
        CreateController(isMultiUserMode: true);
        _controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = "user-alice";

        var documents = new List<Document>
        {
            CreateDocument("doc-1", "user-alice"),
            CreateDocument("doc-2", "user-bob")
        };

        _documentRepository
            .GetByUserAsync("user-alice", Arg.Any<CancellationToken>())
            .Returns(documents);

        var result = await _controller.ExportAll(CancellationToken.None);

        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        var exportedDocuments = await ReadArchiveDocumentsAsync(fileResult.FileStream);

        exportedDocuments.Should().HaveCount(1);
        exportedDocuments.Single().UserId.Should().Be("user-alice");

        await _documentRepository.Received(1).GetByUserAsync("user-alice", Arg.Any<CancellationToken>());
        await _documentRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Import_InMultiUserMode_AssignsImportedManuscriptsToCurrentUser()
    {
        CreateController(isMultiUserMode: true);
        _controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = "user-alice";

        var importedDocuments = new List<Document>
        {
            CreateDocument("source-1", "user-bob"),
            CreateDocument("source-2", "user-charlie")
        };

        await using var fileStream = await BuildArchiveStreamAsync(importedDocuments);
        var file = new FormFile(fileStream, 0, fileStream.Length, "file", "manuscripts.zip");

        var result = await _controller.Import(file, CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ImportDocumentsResponse>().Subject;
        response.ImportedCount.Should().Be(2);

        await _documentRepository.Received(2).SaveAsync(
            Arg.Is<Document>(document =>
                document.UserId == "user-alice" &&
                document.Suggestions.All(suggestion =>
                    suggestion.UserId == "user-alice" &&
                    suggestion.DocumentId == document.Id)),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Import_InSingleUserMode_AssignsImportedManuscriptsToCurrentUser()
    {
        _controller.ControllerContext.HttpContext.Request.Headers.Remove("X-User-Id");
        var savedDocuments = new List<Document>();

        _documentRepository
            .When(repo => repo.SaveAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => savedDocuments.Add(callInfo.Arg<Document>()));

        var importedDocuments = new List<Document>
        {
            CreateDocument("source-1", "user-bob"),
            CreateDocument("source-2", "user-charlie")
        };

        await using var fileStream = await BuildArchiveStreamAsync(importedDocuments);
        var file = new FormFile(fileStream, 0, fileStream.Length, "file", "manuscripts.zip");

        var result = await _controller.Import(file, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();

        savedDocuments.Should().HaveCount(2);
        savedDocuments.Select(document => document.UserId)
            .Should()
            .OnlyContain(userId => userId == "_anonymous");
        savedDocuments.SelectMany(document => document.Suggestions)
            .Should()
            .OnlyContain(suggestion => suggestion.UserId == "_anonymous");
    }

    [TestMethod]
    public async Task Import_WithMalformedZip_ReturnsBadRequest()
    {
        await using var fileStream = new MemoryStream("not a zip file"u8.ToArray());
        var file = new FormFile(fileStream, 0, fileStream.Length, "file", "manuscripts.zip");

        var result = await _controller.Import(file, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task Import_WithInvalidJson_ReturnsBadRequest()
    {
        await using var fileStream = await BuildArchiveStreamAsync("{ invalid json");
        var file = new FormFile(fileStream, 0, fileStream.Length, "file", "manuscripts.zip");

        var result = await _controller.Import(file, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    private void CreateController(bool isMultiUserMode)
    {
        _controller = new DocumentsController(
            _documentRepository,
            _sessionRepository,
            _suggestionService,
            _wordDocumentService,
            _suggestionMergeService,
            _logger,
            isMultiUserModeProvider: () => isMultiUserMode);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private static async Task<MemoryStream> BuildArchiveStreamAsync(IReadOnlyList<Document> documents)
    {
        var archiveStream = new MemoryStream();

        using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("manuscripts.json");
            await using var entryStream = entry.Open();
            await JsonSerializer.SerializeAsync(entryStream, documents, ArchiveSerializerOptions);
        }

        archiveStream.Position = 0;
        return archiveStream;
    }

    private static async Task<MemoryStream> BuildArchiveStreamAsync(string manuscriptsJson)
    {
        var archiveStream = new MemoryStream();

        using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("manuscripts.json");
            await using var writer = new StreamWriter(entry.Open());
            await writer.WriteAsync(manuscriptsJson);
        }

        archiveStream.Position = 0;
        return archiveStream;
    }

    private static async Task<IReadOnlyList<Document>> ReadArchiveDocumentsAsync(Stream archiveStream)
    {
        archiveStream.Position = 0;
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry("manuscripts.json");
        entry.Should().NotBeNull();

        await using var entryStream = entry!.Open();
        var documents = await JsonSerializer.DeserializeAsync<List<Document>>(entryStream, ArchiveSerializerOptions);
        documents.Should().NotBeNull();
        return documents!;
    }

    private static Document CreateDocument(string id, string userId)
    {
        var paragraph = new Paragraph
        {
            Id = "paragraph-1",
            Text = "A sentence with room for improvement."
        };

        var suggestion = new Suggestion
        {
            Id = "suggestion-1",
            UserId = userId,
            DocumentId = id,
            ParagraphId = paragraph.Id,
            Rationale = "Improve clarity.",
            ProposedChange = "A clearer sentence with improved wording.",
            Status = SuggestionStatus.Pending
        };

        return new Document
        {
            Id = id,
            UserId = userId,
            Filename = $"{id}.docx",
            Source = DocumentSource.Local,
            Title = id,
            Status = DocumentStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Paragraphs = [paragraph],
            Suggestions = [suggestion]
        };
    }

}
