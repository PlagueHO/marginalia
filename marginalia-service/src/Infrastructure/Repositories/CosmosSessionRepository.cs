using System.Net;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Marginalia.Infrastructure.Repositories;

/// <summary>
/// Cosmos DB implementation for session storage.
/// </summary>
public sealed class CosmosSessionRepository : ISessionRepository
{
    private readonly Container _container;
    private readonly ILogger<CosmosSessionRepository> _logger;

    public CosmosSessionRepository(CosmosClient cosmosClient, ILogger<CosmosSessionRepository> logger)
    {
        _container = cosmosClient.GetContainer("marginalia", "sessions");
        _logger = logger;
    }

    public async Task<UserSession?> GetByIdAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<UserSession>(
                sessionId,
                new PartitionKey(userId),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Session retrieved from Cosmos: {SessionId}, UserId: {UserId}", sessionId, userId);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Session not found in Cosmos: {SessionId}, UserId: {UserId}", sessionId, userId);
            return null;
        }
    }

    public async Task SaveAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        await _container.UpsertItemAsync(
            session,
            new PartitionKey(session.UserId),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Session saved to Cosmos: {SessionId}, UserId: {UserId}", session.SessionId, session.UserId);
    }

    public async Task AddDocumentToSessionAsync(string userId, string sessionId, string documentId, CancellationToken cancellationToken = default)
    {
        var session = await GetByIdAsync(userId, sessionId, cancellationToken);
        if (session is null)
        {
            session = new UserSession
            {
                SessionId = sessionId,
                UserId = userId,
                DocumentIds = [documentId],
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        else if (!session.DocumentIds.Contains(documentId))
        {
            var updatedIds = session.DocumentIds.Append(documentId).ToList().AsReadOnly();
            session = session with { DocumentIds = updatedIds };
        }
        else
        {
            // Document already in session, no update needed
            return;
        }

        await SaveAsync(session, cancellationToken);
        _logger.LogInformation("Document added to session: {SessionId}, DocumentId: {DocumentId}, UserId: {UserId}", sessionId, documentId, userId);
    }
}
