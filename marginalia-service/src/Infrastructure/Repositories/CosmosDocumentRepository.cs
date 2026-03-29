using System.Net;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Marginalia.Infrastructure.Repositories;

/// <summary>
/// Cosmos DB implementation for document storage.
/// </summary>
public sealed class CosmosDocumentRepository : IDocumentRepository
{
    private readonly Container _container;
    private readonly ILogger<CosmosDocumentRepository> _logger;

    public CosmosDocumentRepository(CosmosClient cosmosClient, ILogger<CosmosDocumentRepository> logger)
    {
        _container = cosmosClient.GetContainer("marginalia", "documents");
        _logger = logger;
    }

    public async Task<Document?> GetByIdAsync(string userId, string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<Document>(
                id,
                new PartitionKey(userId),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Document retrieved from Cosmos: {DocumentId}, UserId: {UserId}", id, userId);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Document not found in Cosmos: {DocumentId}, UserId: {UserId}", id, userId);
            return null;
        }
    }

    public async Task<IReadOnlyList<Document>> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
            .WithParameter("@userId", userId);

        var iterator = _container.GetItemQueryIterator<Document>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId) });

        var documents = new List<Document>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            documents.AddRange(response);
        }

        _logger.LogInformation("Retrieved {Count} documents from Cosmos for UserId: {UserId}", documents.Count, userId);
        return documents.AsReadOnly();
    }

    public async Task SaveAsync(Document document, CancellationToken cancellationToken = default)
    {
        await _container.UpsertItemAsync(
            document,
            new PartitionKey(document.UserId),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Document saved to Cosmos: {DocumentId}, UserId: {UserId}", document.Id, document.UserId);
    }

    public async Task DeleteAsync(string userId, string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.DeleteItemAsync<Document>(
                id,
                new PartitionKey(userId),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Document deleted from Cosmos: {DocumentId}, UserId: {UserId}", id, userId);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Document not found for deletion: {DocumentId}, UserId: {UserId}", id, userId);
        }
    }
}
