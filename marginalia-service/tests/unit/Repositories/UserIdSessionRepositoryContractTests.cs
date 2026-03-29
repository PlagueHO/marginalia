using System.Collections.Concurrent;
using FluentAssertions;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Repositories;

/// <summary>
/// Tests the ISessionRepository contract with userId partitioning.
/// Validates multi-tenant behavior where sessions are partitioned by userId.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public sealed class UserIdSessionRepositoryContractTests
{
    /// <summary>
    /// Test double implementing ISessionRepository with userId partitioning.
    /// </summary>
    private sealed class TestUserIdSessionRepository : ISessionRepository
    {
        private readonly ConcurrentDictionary<string, UserSession> _sessions = new();

        public Task<UserSession?> GetByIdAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
        {
            var key = $"{userId}:{sessionId}";
            _sessions.TryGetValue(key, out var session);
            return Task.FromResult(session);
        }

        public Task SaveAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            var userId = session.UserId ?? "_anonymous";
            var key = $"{userId}:{session.SessionId}";
            _sessions[key] = session;
            return Task.CompletedTask;
        }

        public Task AddDocumentToSessionAsync(string userId, string sessionId, string documentId, CancellationToken cancellationToken = default)
        {
            var key = $"{userId}:{sessionId}";
            _sessions.AddOrUpdate(
                key,
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

    private static UserSession CreateSession(string sessionId = "session-1", string userId = "_anonymous") => new()
    {
        SessionId = sessionId,
        UserId = userId,
        DocumentIds = [],
        Timestamp = DateTimeOffset.UtcNow
    };

    private TestUserIdSessionRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new TestUserIdSessionRepository();
    }

    [TestMethod]
    public async Task GetByIdAsync_SessionNotFound_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync("user-1", "nonexistent");
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task SaveAsync_ThenGetByIdAsync_ReturnsSession()
    {
        var session = CreateSession("session-1", "user-1");

        await _repository.SaveAsync(session);
        var retrieved = await _repository.GetByIdAsync("user-1", "session-1");

        retrieved.Should().NotBeNull();
        retrieved!.SessionId.Should().Be("session-1");
        retrieved.UserId.Should().Be("user-1");
    }

    [TestMethod]
    public async Task SaveAsync_WithUserId_StoresCorrectly()
    {
        var session = CreateSession("session-1", "user-alice");
        await _repository.SaveAsync(session);

        var retrieved = await _repository.GetByIdAsync("user-alice", "session-1");
        retrieved.Should().NotBeNull();
        retrieved!.UserId.Should().Be("user-alice");
    }

    [TestMethod]
    public async Task GetByIdAsync_DifferentUserSameSessionId_ReturnsNull()
    {
        var session = CreateSession("session-1", "user-alice");
        await _repository.SaveAsync(session);

        var retrieved = await _repository.GetByIdAsync("user-bob", "session-1");
        retrieved.Should().BeNull("user-bob should not see user-alice's session");
    }

    [TestMethod]
    public async Task SaveAsync_OverwritesExistingSession()
    {
        var original = CreateSession("session-1", "user-1");
        await _repository.SaveAsync(original);

        var updated = original with { DocumentIds = ["doc-1", "doc-2"] };
        await _repository.SaveAsync(updated);

        var retrieved = await _repository.GetByIdAsync("user-1", "session-1");
        retrieved!.DocumentIds.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task AddDocumentToSessionAsync_CreatesNewSession()
    {
        await _repository.AddDocumentToSessionAsync("user-1", "session-1", "doc-1");

        var retrieved = await _repository.GetByIdAsync("user-1", "session-1");
        retrieved.Should().NotBeNull();
        retrieved!.DocumentIds.Should().Contain("doc-1");
        retrieved.UserId.Should().Be("user-1");
    }

    [TestMethod]
    public async Task AddDocumentToSessionAsync_AppendsToExistingSession()
    {
        var session = CreateSession("session-1", "user-1") with { DocumentIds = ["doc-1"] };
        await _repository.SaveAsync(session);

        await _repository.AddDocumentToSessionAsync("user-1", "session-1", "doc-2");

        var retrieved = await _repository.GetByIdAsync("user-1", "session-1");
        retrieved!.DocumentIds.Should().HaveCount(2);
        retrieved.DocumentIds.Should().Contain(["doc-1", "doc-2"]);
    }

    [TestMethod]
    public async Task AddDocumentToSessionAsync_IgnoresDuplicates()
    {
        var session = CreateSession("session-1", "user-1") with { DocumentIds = ["doc-1"] };
        await _repository.SaveAsync(session);

        await _repository.AddDocumentToSessionAsync("user-1", "session-1", "doc-1");

        var retrieved = await _repository.GetByIdAsync("user-1", "session-1");
        retrieved!.DocumentIds.Should().ContainSingle();
    }

    [TestMethod]
    public async Task AddDocumentToSessionAsync_DifferentUser_CreatesIsolatedSession()
    {
        await _repository.AddDocumentToSessionAsync("user-alice", "session-1", "doc-1");
        await _repository.AddDocumentToSessionAsync("user-bob", "session-1", "doc-2");

        var aliceSession = await _repository.GetByIdAsync("user-alice", "session-1");
        var bobSession = await _repository.GetByIdAsync("user-bob", "session-1");

        aliceSession.Should().NotBeNull();
        aliceSession!.DocumentIds.Should().Contain("doc-1");
        aliceSession.DocumentIds.Should().NotContain("doc-2");

        bobSession.Should().NotBeNull();
        bobSession!.DocumentIds.Should().Contain("doc-2");
        bobSession.DocumentIds.Should().NotContain("doc-1");
    }

    [TestMethod]
    public async Task ConcurrentAddDocumentToSession_DoesNotLoseDocuments()
    {
        var tasks = Enumerable.Range(1, 50)
            .Select(i => _repository.AddDocumentToSessionAsync("user-1", "session-1", $"doc-{i}"));

        await Task.WhenAll(tasks);

        var retrieved = await _repository.GetByIdAsync("user-1", "session-1");
        retrieved!.DocumentIds.Should().HaveCount(50);
    }

    [TestMethod]
    public async Task ConcurrentSaves_WithDifferentUsers_DoNotInterfere()
    {
        var tasks = Enumerable.Range(1, 25)
            .SelectMany(i => new[]
            {
                _repository.SaveAsync(CreateSession($"session-{i}", "user-alice")),
                _repository.SaveAsync(CreateSession($"session-{i}", "user-bob"))
            });

        await Task.WhenAll(tasks);

        for (var i = 1; i <= 25; i++)
        {
            var aliceSession = await _repository.GetByIdAsync("user-alice", $"session-{i}");
            var bobSession = await _repository.GetByIdAsync("user-bob", $"session-{i}");

            aliceSession.Should().NotBeNull();
            aliceSession!.UserId.Should().Be("user-alice");

            bobSession.Should().NotBeNull();
            bobSession!.UserId.Should().Be("user-bob");
        }
    }

    [TestMethod]
    public async Task GetByIdAsync_WithCancellation_RespectsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => _repository.GetByIdAsync("user-1", "session-1", cts.Token);
        await act.Should().NotThrowAsync("test double completes synchronously");
    }
}
