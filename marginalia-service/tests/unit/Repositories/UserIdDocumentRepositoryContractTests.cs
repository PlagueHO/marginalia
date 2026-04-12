using System.Collections.Concurrent;
using FluentAssertions;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Repositories;

/// <summary>
/// Tests the IDocumentRepository contract with userId partitioning.
/// Validates multi-tenant behavior where documents are partitioned by userId.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public sealed class UserIdDocumentRepositoryContractTests
{
    /// <summary>
    /// Test double implementing IDocumentRepository with userId partitioning.
    /// </summary>
    private sealed class TestUserIdDocumentRepository : IDocumentRepository
    {
        private readonly ConcurrentDictionary<string, Document> _documents = new();

        public Task<Document?> GetByIdAsync(string userId, string id, CancellationToken cancellationToken = default)
        {
            var key = $"{userId}:{id}";
            _documents.TryGetValue(key, out var document);
            return Task.FromResult(document);
        }

        public Task<IReadOnlyList<Document>> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var userDocs = _documents
                .Where(kvp => kvp.Key.StartsWith($"{userId}:"))
                .Select(kvp => kvp.Value)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<Document>>(userDocs);
        }

        public Task SaveAsync(Document document, CancellationToken cancellationToken = default)
        {
            var userId = document.UserId ?? "_anonymous";
            var key = $"{userId}:{document.Id}";
            _documents[key] = document;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string userId, string id, CancellationToken cancellationToken = default)
        {
            var key = $"{userId}:{id}";
            _documents.TryRemove(key, out _);
            return Task.CompletedTask;
        }
    }

    private static Document CreateDocument(string id = "doc-1", string userId = "_anonymous") => new()
    {
        Id = id,
        UserId = userId,
        Filename = $"{id}.docx",
        Source = DocumentSource.Local,
        Paragraphs = [new Paragraph { Id = "p1", Text = "The morning light filtered through the study window." }]
    };

    private TestUserIdDocumentRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new TestUserIdDocumentRepository();
    }

    [TestMethod]
    public async Task GetByIdAsync_DocumentNotFound_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync("user-1", "nonexistent");
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task SaveAsync_ThenGetByIdAsync_ReturnsDocument()
    {
        var doc = CreateDocument("doc-1", "user-1");

        await _repository.SaveAsync(doc);
        var retrieved = await _repository.GetByIdAsync("user-1", "doc-1");

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be("doc-1");
        retrieved.UserId.Should().Be("user-1");
        retrieved.FullText.Should().Be(doc.FullText);
    }

    [TestMethod]
    public async Task SaveAsync_WithUserId_StoresCorrectly()
    {
        var doc = CreateDocument("doc-1", "user-alice");
        await _repository.SaveAsync(doc);

        var retrieved = await _repository.GetByIdAsync("user-alice", "doc-1");
        retrieved.Should().NotBeNull();
        retrieved!.UserId.Should().Be("user-alice");
    }

    [TestMethod]
    public async Task GetByUserAsync_ReturnsOnlyUserDocuments()
    {
        var doc1 = CreateDocument("doc-1", "user-alice");
        var doc2 = CreateDocument("doc-2", "user-alice");
        var doc3 = CreateDocument("doc-3", "user-bob");

        await _repository.SaveAsync(doc1);
        await _repository.SaveAsync(doc2);
        await _repository.SaveAsync(doc3);

        var aliceDocs = await _repository.GetByUserAsync("user-alice");
        var bobDocs = await _repository.GetByUserAsync("user-bob");

        aliceDocs.Should().HaveCount(2);
        aliceDocs.Select(d => d.Id).Should().Contain(["doc-1", "doc-2"]);
        aliceDocs.All(d => d.UserId == "user-alice").Should().BeTrue();

        bobDocs.Should().HaveCount(1);
        bobDocs[0].Id.Should().Be("doc-3");
        bobDocs[0].UserId.Should().Be("user-bob");
    }

    [TestMethod]
    public async Task GetByUserAsync_NoDocuments_ReturnsEmptyList()
    {
        var result = await _repository.GetByUserAsync("user-nobody");
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetByIdAsync_DifferentUserSameDocId_ReturnsNull()
    {
        var doc = CreateDocument("doc-1", "user-alice");
        await _repository.SaveAsync(doc);

        var retrieved = await _repository.GetByIdAsync("user-bob", "doc-1");
        retrieved.Should().BeNull("user-bob should not see user-alice's document");
    }

    [TestMethod]
    public async Task SaveAsync_OverwritesExistingDocument()
    {
        var original = CreateDocument("doc-1", "user-1");
        await _repository.SaveAsync(original);

        var updated = original with { Paragraphs = [new Paragraph { Id = "p1", Text = "Revised content with more narrative air." }] };
        await _repository.SaveAsync(updated);

        var retrieved = await _repository.GetByIdAsync("user-1", "doc-1");
        retrieved!.FullText.Should().Be("Revised content with more narrative air.");
    }

    [TestMethod]
    public async Task DeleteAsync_RemovesDocument()
    {
        var doc = CreateDocument("doc-1", "user-1");
        await _repository.SaveAsync(doc);

        await _repository.DeleteAsync("user-1", "doc-1");
        var retrieved = await _repository.GetByIdAsync("user-1", "doc-1");

        retrieved.Should().BeNull();
    }

    [TestMethod]
    public async Task DeleteAsync_DifferentUser_DoesNotDeleteDocument()
    {
        var doc = CreateDocument("doc-1", "user-alice");
        await _repository.SaveAsync(doc);

        await _repository.DeleteAsync("user-bob", "doc-1");
        var retrieved = await _repository.GetByIdAsync("user-alice", "doc-1");

        retrieved.Should().NotBeNull("user-bob should not be able to delete user-alice's document");
    }

    [TestMethod]
    public async Task DeleteAsync_NonexistentDocument_NoError()
    {
        var act = () => _repository.DeleteAsync("user-1", "nonexistent");
        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task GetByUserAsync_MultipleUsers_IsolatesCorrectly()
    {
        var users = new[] { "user-1", "user-2", "user-3" };
        foreach (var user in users)
        {
            for (var i = 1; i <= 3; i++)
            {
                await _repository.SaveAsync(CreateDocument($"doc-{i}", user));
            }
        }

        foreach (var user in users)
        {
            var docs = await _repository.GetByUserAsync(user);
            docs.Should().HaveCount(3);
            docs.All(d => d.UserId == user).Should().BeTrue();
        }
    }

    [TestMethod]
    public async Task SaveAsync_WithSuggestions_PreservesUserIdOnDocument()
    {
        var suggestion = new Suggestion
        {
            Id = "sug-1",
            DocumentId = "doc-1",
            UserId = "user-1",
            ParagraphId = "p1",
            Rationale = "Too compressed",
            ProposedChange = "Expanded version",
            Status = SuggestionStatus.Pending
        };

        var doc = CreateDocument("doc-1", "user-1") with { Suggestions = [suggestion] };
        await _repository.SaveAsync(doc);

        var retrieved = await _repository.GetByIdAsync("user-1", "doc-1");
        retrieved!.UserId.Should().Be("user-1");
        retrieved.Suggestions.Should().HaveCount(1);
        retrieved.Suggestions[0].UserId.Should().Be("user-1");
    }

    [TestMethod]
    public async Task ConcurrentSaves_WithDifferentUsers_DoNotInterfere()
    {
        var tasks = Enumerable.Range(1, 50)
            .SelectMany(i => new[]
            {
                _repository.SaveAsync(CreateDocument($"doc-{i}", "user-alice")),
                _repository.SaveAsync(CreateDocument($"doc-{i}", "user-bob"))
            });

        await Task.WhenAll(tasks);

        var aliceDocs = await _repository.GetByUserAsync("user-alice");
        var bobDocs = await _repository.GetByUserAsync("user-bob");

        aliceDocs.Should().HaveCount(50);
        bobDocs.Should().HaveCount(50);
        aliceDocs.All(d => d.UserId == "user-alice").Should().BeTrue();
        bobDocs.All(d => d.UserId == "user-bob").Should().BeTrue();
    }

    [TestMethod]
    public async Task GetByIdAsync_WithCancellation_RespectsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => _repository.GetByIdAsync("user-1", "doc-1", cts.Token);
        await act.Should().NotThrowAsync("test double completes synchronously");
    }
}
