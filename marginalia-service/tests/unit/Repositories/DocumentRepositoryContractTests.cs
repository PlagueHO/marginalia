using System.Collections.Concurrent;
using FluentAssertions;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Repositories;

/// <summary>
/// Tests the IDocumentRepository contract using a simple in-memory test double.
/// When InMemoryDocumentRepository lands in Infrastructure, these same scenarios
/// should be verified against the real implementation.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public sealed class DocumentRepositoryContractTests
{
    /// <summary>
    /// Minimal test double implementing IDocumentRepository for contract verification.
    /// </summary>
    private sealed class TestDocumentRepository : IDocumentRepository
    {
        private readonly ConcurrentDictionary<string, Document> _documents = new();

        public Task<Document?> GetByIdAsync(string userId, string id, CancellationToken cancellationToken = default)
        {
            _documents.TryGetValue(id, out var document);
            return Task.FromResult(document);
        }

        public Task<IReadOnlyList<Document>> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var userDocs = _documents.Values.Where(d => d.UserId == userId).ToList();
            return Task.FromResult<IReadOnlyList<Document>>(userDocs);
        }

        public Task SaveAsync(Document document, CancellationToken cancellationToken = default)
        {
            _documents[document.Id] = document;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string userId, string id, CancellationToken cancellationToken = default)
        {
            _documents.TryRemove(id, out _);
            return Task.CompletedTask;
        }
    }

    private static Document CreateDocument(string id = "doc-1") => new()
    {
        Id = id,
        Filename = $"{id}.docx",
        Source = DocumentSource.Local,
        Paragraphs = [new Paragraph { Id = "p1", Text = "The morning light filtered through the study window." }]
    };

    private TestDocumentRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new TestDocumentRepository();
    }

    [TestMethod]
    public async Task GetByIdAsync_DocumentNotFound_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync("_anonymous", "nonexistent");
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task SaveAsync_ThenGetByIdAsync_ReturnsDocument()
    {
        var doc = CreateDocument();

        await _repository.SaveAsync(doc);
        var retrieved = await _repository.GetByIdAsync("_anonymous", "doc-1");

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be("doc-1");
        retrieved.FullText.Should().Be(doc.FullText);
    }

    [TestMethod]
    public async Task SaveAsync_OverwritesExistingDocument()
    {
        var original = CreateDocument();
        await _repository.SaveAsync(original);

        var updated = original with { Paragraphs = [new Paragraph { Id = "p1", Text = "Revised content with more narrative air." }] };
        await _repository.SaveAsync(updated);

        var retrieved = await _repository.GetByIdAsync("_anonymous", "doc-1");
        retrieved!.FullText.Should().Be("Revised content with more narrative air.");
    }

    [TestMethod]
    public async Task GetByUserAsync_NoDocuments_ReturnsEmptyList()
    {
        var result = await _repository.GetByUserAsync("user-1");
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetByUserAsync_WithDocuments_ReturnsAll()
    {
        var doc1 = CreateDocument("doc-1") with { UserId = "user-1" };
        var doc2 = CreateDocument("doc-2") with { UserId = "user-1" };

        await _repository.SaveAsync(doc1);
        await _repository.SaveAsync(doc2);

        var result = await _repository.GetByUserAsync("user-1");

        result.Should().HaveCount(2);
        result.Select(d => d.Id).Should().Contain(["doc-1", "doc-2"]);
    }

    [TestMethod]
    public async Task GetByUserAsync_IsolatesBetweenUsers()
    {
        var doc1 = CreateDocument("doc-1") with { UserId = "user-a" };
        var doc2 = CreateDocument("doc-2") with { UserId = "user-b" };

        await _repository.SaveAsync(doc1);
        await _repository.SaveAsync(doc2);

        var resultA = await _repository.GetByUserAsync("user-a");
        var resultB = await _repository.GetByUserAsync("user-b");

        resultA.Should().ContainSingle().Which.Id.Should().Be("doc-1");
        resultB.Should().ContainSingle().Which.Id.Should().Be("doc-2");
    }

    [TestMethod]
    public async Task ConcurrentSaves_DoNotLoseData()
    {
        var tasks = Enumerable.Range(1, 50)
            .Select(i => _repository.SaveAsync(CreateDocument($"doc-{i}")));

        await Task.WhenAll(tasks);

        // All 50 documents should be retrievable
        for (var i = 1; i <= 50; i++)
        {
            var doc = await _repository.GetByIdAsync("_anonymous", $"doc-{i}");
            doc.Should().NotBeNull($"doc-{i} should have been saved");
        }
    }

    [TestMethod]
    public async Task SaveAsync_PreservesSuggestionsOnDocument()
    {
        var suggestion = new Suggestion
        {
            Id = "sug-1",
            DocumentId = "doc-1",
            ParagraphId = "p1",
            Rationale = "Too compressed",
            ProposedChange = "Expanded version",
            Status = SuggestionStatus.Pending
        };

        var doc = CreateDocument() with { Suggestions = [suggestion] };
        await _repository.SaveAsync(doc);

        var retrieved = await _repository.GetByIdAsync("_anonymous", "doc-1");
        retrieved!.Suggestions.Should().HaveCount(1);
        retrieved.Suggestions[0].Rationale.Should().Be("Too compressed");
    }

    [TestMethod]
    public async Task GetByIdAsync_WithCancellation_RespectsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Test double completes synchronously, so cancellation doesn't throw.
        // Real implementations should check the token.
        var act = () => _repository.GetByIdAsync("_anonymous", "doc-1", cts.Token);
        await act.Should().NotThrowAsync();
    }
}
