using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Marginalia.Domain.Interfaces;
using Marginalia.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Marginalia.Api.IntegrationTests.Controllers;

[TestClass]
[TestCategory("Integration")]
public sealed class ExportDownloadCleanupIntegrationTests : IDisposable
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
                    var cosmosDocDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IDocumentRepository));
                    if (cosmosDocDesc != null) services.Remove(cosmosDocDesc);
                    services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();

                    var cosmosSessionDesc = services.FirstOrDefault(d => d.ServiceType == typeof(ISessionRepository));
                    if (cosmosSessionDesc != null) services.Remove(cosmosSessionDesc);
                    services.AddSingleton<ISessionRepository, InMemorySessionRepository>();

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
    public async Task DownloadExport_DeletesTemporaryArtifactAfterResponseCompletes()
    {
        const string userId = "cleanup-integration-user";

        var createContent = new StringContent(
            JsonSerializer.Serialize(new { Content = "Cleanup test content", Filename = "cleanup.txt" }),
            Encoding.UTF8,
            "application/json");

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/documents/paste")
        {
            Content = createContent
        };
        createRequest.Headers.Add("X-User-Id", userId);

        var createResponse = await _client.SendAsync(createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var startRequest = new HttpRequestMessage(HttpMethod.Post, "/api/exports");
        startRequest.Headers.Add("X-User-Id", userId);

        var startResponse = await _client.SendAsync(startRequest);
        startResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var startBody = await startResponse.Content.ReadFromJsonAsync<StartJobResponse>();
        startBody.Should().NotBeNull();
        startBody!.JobId.Should().NotBeNullOrWhiteSpace();

        ExportJobResponse? job = null;
        for (var i = 0; i < 50; i++)
        {
            var statusRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/exports/{startBody.JobId}");
            statusRequest.Headers.Add("X-User-Id", userId);

            var statusResponse = await _client.SendAsync(statusRequest);
            statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            job = await statusResponse.Content.ReadFromJsonAsync<ExportJobResponse>();
            if (job is { Status: "Completed" or "Failed" })
            {
                break;
            }

            await Task.Delay(100);
        }

        job.Should().NotBeNull();
        job!.Status.Should().Be("Completed");

        var expectedArtifactPath = Path.Combine(Path.GetTempPath(), $"marginalia-export-{startBody.JobId}.zip");
        File.Exists(expectedArtifactPath).Should().BeTrue("the completed export should materialize an artifact before download");

        var downloadRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/exports/{startBody.JobId}/download");
        downloadRequest.Headers.Add("X-User-Id", userId);

        var downloadResponse = await _client.SendAsync(downloadRequest, HttpCompletionOption.ResponseHeadersRead);
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await downloadResponse.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(0);
        downloadResponse.Dispose();

        var deleted = false;
        for (var i = 0; i < 30; i++)
        {
            if (!File.Exists(expectedArtifactPath))
            {
                deleted = true;
                break;
            }

            await Task.Delay(100);
        }

        deleted.Should().BeTrue("the download completion callback should delete temporary export artifacts");
    }

    private sealed record StartJobResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("jobId")]
        public required string JobId { get; init; }
    }

    private sealed record ExportJobResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public required string Status { get; init; }
    }
}
