using System.Text.Json;
using FluentAssertions;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Domain;

[TestClass]
[TestCategory("Unit")]
public sealed class UserSessionTests
{
    [TestMethod]
    public void Constructor_WithRequiredFields_CreatesSession()
    {
        var now = DateTimeOffset.UtcNow;
        var session = new UserSession
        {
            SessionId = "session-abc",
            DocumentIds = ["doc-1", "doc-2"],
            Timestamp = now
        };

        session.SessionId.Should().Be("session-abc");
        session.DocumentIds.Should().HaveCount(2);
        session.Timestamp.Should().Be(now);
    }

    [TestMethod]
    public void DocumentIds_EmptyList_IsValidForNewSession()
    {
        var session = new UserSession
        {
            SessionId = "session-new",
            DocumentIds = [],
            Timestamp = DateTimeOffset.UtcNow
        };

        session.DocumentIds.Should().BeEmpty();
    }

    [TestMethod]
    public void DocumentIds_SingleDocument_IsCommonCase()
    {
        var session = new UserSession
        {
            SessionId = "session-1",
            DocumentIds = ["doc-only"],
            Timestamp = DateTimeOffset.UtcNow
        };

        session.DocumentIds.Should().ContainSingle()
            .Which.Should().Be("doc-only");
    }

    [TestMethod]
    public void DocumentIds_MultipleDocuments_UpToTenPagesPerSession()
    {
        var docIds = Enumerable.Range(1, 5).Select(i => $"doc-{i}").ToList();
        var session = new UserSession
        {
            SessionId = "session-multi",
            DocumentIds = docIds,
            Timestamp = DateTimeOffset.UtcNow
        };

        session.DocumentIds.Should().HaveCount(5);
    }

    [TestMethod]
    public void Serialization_ProducesCamelCasePropertyNames()
    {
        var session = new UserSession
        {
            SessionId = "session-1",
            DocumentIds = ["doc-1"],
            Timestamp = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(session);

        json.Should().Contain("\"sessionId\":");
        json.Should().Contain("\"documentIds\":");
        json.Should().Contain("\"timestamp\":");
    }

    [TestMethod]
    public void Deserialization_FromCamelCaseJson_ReconstructsSession()
    {
        const string json = """
        {
            "sessionId": "session-42",
            "documentIds": ["doc-a", "doc-b"],
            "timestamp": "2026-03-22T10:30:00+00:00"
        }
        """;

        var session = JsonSerializer.Deserialize<UserSession>(json);

        session.Should().NotBeNull();
        session!.SessionId.Should().Be("session-42");
        session.DocumentIds.Should().HaveCount(2);
        session.Timestamp.Year.Should().Be(2026);
    }

    [TestMethod]
    public void Record_Equality_MatchesOnAllProperties()
    {
        var timestamp = DateTimeOffset.UtcNow;
        IReadOnlyList<string> docIds = new List<string> { "doc-1" };

        var session1 = new UserSession
        {
            SessionId = "session-1",
            DocumentIds = docIds,
            Timestamp = timestamp
        };

        var session2 = new UserSession
        {
            SessionId = "session-1",
            DocumentIds = docIds,
            Timestamp = timestamp
        };

        session1.Should().Be(session2);
    }

    [TestMethod]
    public void Record_With_CreatesModifiedCopy()
    {
        var original = new UserSession
        {
            SessionId = "session-1",
            DocumentIds = ["doc-1"],
            Timestamp = DateTimeOffset.UtcNow
        };

        var updated = original with { DocumentIds = ["doc-1", "doc-2"] };

        updated.DocumentIds.Should().HaveCount(2);
        original.DocumentIds.Should().HaveCount(1);
    }
}
