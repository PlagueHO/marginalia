using Marginalia.Domain.Models;

namespace Marginalia.Domain.Interfaces;

/// <summary>
/// Contract for document storage and retrieval.
/// </summary>
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(string userId, string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task SaveAsync(Document document, CancellationToken cancellationToken = default);
    Task DeleteAsync(string userId, string id, CancellationToken cancellationToken = default);
}
