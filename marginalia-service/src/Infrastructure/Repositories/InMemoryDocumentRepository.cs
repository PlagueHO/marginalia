using System.Collections.Concurrent;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;

namespace Marginalia.Infrastructure.Repositories;

/// <summary>
/// Thread-safe in-memory document storage. No database — per spec.
/// </summary>
public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly ConcurrentDictionary<string, Document> _documents = new();

    public Task<Document?> GetByIdAsync(string userId, string id, CancellationToken cancellationToken = default)
    {
        _documents.TryGetValue(id, out var document);
        // Enforce userId-based access (mirrors Cosmos partition key behavior)
        if (document is not null && document.UserId != userId)
        {
            return Task.FromResult<Document?>(null);
        }
        return Task.FromResult(document);
    }

    public Task<IReadOnlyList<Document>> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var userDocs = _documents.Values.Where(d => d.UserId == userId).ToList().AsReadOnly();
        return Task.FromResult<IReadOnlyList<Document>>(userDocs);
    }

    public Task SaveAsync(Document document, CancellationToken cancellationToken = default)
    {
        _documents.AddOrUpdate(document.Id, document, (_, _) => document);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string userId, string id, CancellationToken cancellationToken = default)
    {
        if (_documents.TryGetValue(id, out var doc) && doc.UserId == userId)
        {
            _documents.TryRemove(id, out _);
        }
        return Task.CompletedTask;
    }
}
