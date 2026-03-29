using System.Collections.Concurrent;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;

namespace Marginalia.Infrastructure.Repositories;

/// <summary>
/// Thread-safe in-memory session storage.
/// </summary>
public sealed class InMemorySessionRepository : ISessionRepository
{
    private readonly ConcurrentDictionary<string, UserSession> _sessions = new();

    public Task<UserSession?> GetByIdAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId, out var session);
        if (session is not null && session.UserId != userId)
        {
            return Task.FromResult<UserSession?>(null);
        }
        return Task.FromResult(session);
    }

    public Task SaveAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        _sessions.AddOrUpdate(session.SessionId, session, (_, _) => session);
        return Task.CompletedTask;
    }

    public Task AddDocumentToSessionAsync(string userId, string sessionId, string documentId, CancellationToken cancellationToken = default)
    {
        _sessions.AddOrUpdate(
            sessionId,
            _ => new UserSession
            {
                SessionId = sessionId,
                UserId = userId,
                DocumentIds = [documentId],
                Timestamp = DateTimeOffset.UtcNow
            },
            (_, existing) =>
            {
                if (existing.DocumentIds.Contains(documentId))
                {
                    return existing;
                }

                var updatedIds = existing.DocumentIds.Append(documentId).ToList().AsReadOnly();
                return existing with { DocumentIds = updatedIds };
            });

        return Task.CompletedTask;
    }
}
