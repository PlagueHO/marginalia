using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Marginalia.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Marginalia.Tests.Integration.Controllers;

/// <summary>
/// Tests that controllers correctly extract userId from X-User-Id header
/// and default to "_anonymous" when not provided.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public sealed class UserIdHeaderExtractionTests : IDisposable
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace Cosmos repos with in-memory for integration tests
                    var cosmosDocDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IDocumentRepository));
                    if (cosmosDocDesc != null) services.Remove(cosmosDocDesc);
                    services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();

                    var cosmosSessionDesc = services.FirstOrDefault(d => d.ServiceType == typeof(ISessionRepository));
                    if (cosmosSessionDesc != null) services.Remove(cosmosSessionDesc);
                    services.AddSingleton<ISessionRepository, InMemorySessionRepository>();

                    // Register a no-op IChatClient so FoundrySuggestionService can resolve
                    var chatClientDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IChatClient));
                    if (chatClientDesc == null)
                    {
                        services.AddSingleton<IChatClient>(new NoOpChatClient());
                    }
                });
            });
        _client = _factory.CreateClient();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    [TestMethod]
    public async Task DocumentsController_WithXUserIdHeader_UsesHeaderValue()
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { Content = "Test content", Filename = "test.txt" }),
            Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/documents/paste")
        {
            Content = content
        };
        request.Headers.Add("X-User-Id", "user-alice");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<UploadDocumentResponse>();
        result.Should().NotBeNull();
        result!.Document.UserId.Should().Be("user-alice", "controller should extract userId from X-User-Id header");
    }

    [TestMethod]
    public async Task DocumentsController_WithoutXUserIdHeader_DefaultsToAnonymous()
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { Content = "Test content", Filename = "test.txt" }),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/documents/paste", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<UploadDocumentResponse>();
        result.Should().NotBeNull();
        result!.Document.UserId.Should().Be("_anonymous", "controller should default to _anonymous when X-User-Id header is absent");
    }

    [TestMethod]
    public async Task DocumentsController_WithEmptyXUserIdHeader_DefaultsToAnonymous()
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { Content = "Test content", Filename = "test.txt" }),
            Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/documents/paste")
        {
            Content = content
        };
        request.Headers.Add("X-User-Id", "");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<UploadDocumentResponse>();
        result.Should().NotBeNull();
        result!.Document.UserId.Should().Be("_anonymous", "controller should default to _anonymous when X-User-Id header is empty");
    }

    [TestMethod]
    public async Task DocumentsController_WithWhitespaceXUserIdHeader_DefaultsToAnonymous()
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { Content = "Test content", Filename = "test.txt" }),
            Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/documents/paste")
        {
            Content = content
        };
        request.Headers.Add("X-User-Id", "   ");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<UploadDocumentResponse>();
        result.Should().NotBeNull();
        result!.Document.UserId.Should().Be("_anonymous", "controller should default to _anonymous when X-User-Id header is whitespace");
    }

    [TestMethod]
    public async Task SessionsController_WithXUserIdHeader_UsesHeaderValue()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/sessions");
        request.Headers.Add("X-User-Id", "user-bob");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<UserSession>();
        result.Should().NotBeNull();
        result!.UserId.Should().Be("user-bob", "controller should extract userId from X-User-Id header");
    }

    [TestMethod]
    public async Task SessionsController_WithoutXUserIdHeader_DefaultsToAnonymous()
    {
        var response = await _client.PostAsync("/api/sessions", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<UserSession>();
        result.Should().NotBeNull();
        result!.UserId.Should().Be("_anonymous", "controller should default to _anonymous when X-User-Id header is absent");
    }

    [TestMethod]
    public async Task GetDocument_WithXUserIdHeader_OnlyReturnsUserDocument()
    {
        // Create a document for user-alice
        var createContent = new StringContent(
            JsonSerializer.Serialize(new { Content = "Alice's content", Filename = "alice.txt" }),
            Encoding.UTF8,
            "application/json");

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/documents/paste")
        {
            Content = createContent
        };
        createRequest.Headers.Add("X-User-Id", "user-alice");

        var createResponse = await _client.SendAsync(createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<UploadDocumentResponse>();
        var documentId = createResult!.Document.Id;

        // Try to get it as user-bob
        var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/documents/{documentId}");
        getRequest.Headers.Add("X-User-Id", "user-bob");

        var getResponse = await _client.SendAsync(getRequest);

        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound, "user-bob should not see user-alice's document");
    }

    [TestMethod]
    public async Task GetDocument_WithMatchingUserId_ReturnsDocument()
    {
        // Create a document for user-charlie
        var createContent = new StringContent(
            JsonSerializer.Serialize(new { Content = "Charlie's content", Filename = "charlie.txt" }),
            Encoding.UTF8,
            "application/json");

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/documents/paste")
        {
            Content = createContent
        };
        createRequest.Headers.Add("X-User-Id", "user-charlie");

        var createResponse = await _client.SendAsync(createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<UploadDocumentResponse>();
        var documentId = createResult!.Document.Id;

        // Get it as user-charlie
        var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/documents/{documentId}");
        getRequest.Headers.Add("X-User-Id", "user-charlie");

        var getResponse = await _client.SendAsync(getRequest);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await getResponse.Content.ReadFromJsonAsync<Document>();
        document.Should().NotBeNull();
        document!.Id.Should().Be(documentId);
        document.UserId.Should().Be("user-charlie");
    }
}

/// <summary>
/// Response DTO for document upload endpoint.
/// </summary>
public sealed record UploadDocumentResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("document")]
    public required Document Document { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }
}

/// <summary>
/// No-op IChatClient for integration tests where AI Foundry is not available.
/// </summary>
internal sealed class NoOpChatClient : IChatClient
{
    public ChatClientMetadata Metadata => new("NoOp");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "[]")));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Streaming not supported in test stub.");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
