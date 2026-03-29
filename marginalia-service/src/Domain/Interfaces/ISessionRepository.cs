using Marginalia.Domain.Models;

namespace Marginalia.Domain.Interfaces;

/// <summary>
/// Contract for user session storage and retrieval.
/// </summary>
public interface ISessionRepository
{
    Task<UserSession?> GetByIdAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
    Task SaveAsync(UserSession session, CancellationToken cancellationToken = default);
    Task AddDocumentToSessionAsync(string userId, string sessionId, string documentId, CancellationToken cancellationToken = default);
}
