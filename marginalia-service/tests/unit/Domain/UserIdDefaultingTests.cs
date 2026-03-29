using FluentAssertions;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Domain;

/// <summary>
/// Tests that domain models correctly default userId to "_anonymous".
/// </summary>
[TestClass]
[TestCategory("Unit")]
public sealed class UserIdDefaultingTests
{
    [TestMethod]
    public void Document_WithoutUserId_DefaultsToAnonymous()
    {
        var document = new Document
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Content = "Test content"
        };

        document.UserId.Should().Be("_anonymous", "userId should default to _anonymous when not specified");
    }

    [TestMethod]
    public void Document_WithExplicitUserId_PreservesValue()
    {
        var document = new Document
        {
            Id = "doc-1",
            UserId = "user-alice",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Content = "Test content"
        };

        document.UserId.Should().Be("user-alice");
    }

    [TestMethod]
    public void UserSession_WithoutUserId_DefaultsToAnonymous()
    {
        var session = new UserSession
        {
            SessionId = "session-1",
            DocumentIds = [],
            Timestamp = DateTimeOffset.UtcNow
        };

        session.UserId.Should().Be("_anonymous", "userId should default to _anonymous when not specified");
    }

    [TestMethod]
    public void UserSession_WithExplicitUserId_PreservesValue()
    {
        var session = new UserSession
        {
            SessionId = "session-1",
            UserId = "user-bob",
            DocumentIds = [],
            Timestamp = DateTimeOffset.UtcNow
        };

        session.UserId.Should().Be("user-bob");
    }

    [TestMethod]
    public void Suggestion_WithoutUserId_DefaultsToAnonymous()
    {
        var suggestion = new Suggestion
        {
            Id = "sug-1",
            DocumentId = "doc-1",
            TextRange = new TextRange { Start = 0, End = 10 },
            Rationale = "Test rationale",
            ProposedChange = "Test change",
            Status = SuggestionStatus.Pending
        };

        suggestion.UserId.Should().Be("_anonymous", "userId should default to _anonymous when not specified");
    }

    [TestMethod]
    public void Suggestion_WithExplicitUserId_PreservesValue()
    {
        var suggestion = new Suggestion
        {
            Id = "sug-1",
            DocumentId = "doc-1",
            UserId = "user-charlie",
            TextRange = new TextRange { Start = 0, End = 10 },
            Rationale = "Test rationale",
            ProposedChange = "Test change",
            Status = SuggestionStatus.Pending
        };

        suggestion.UserId.Should().Be("user-charlie");
    }

    [TestMethod]
    public void Document_RecordWith_PreservesUserId()
    {
        var original = new Document
        {
            Id = "doc-1",
            UserId = "user-alice",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Content = "Original content"
        };

        var updated = original with { Content = "Updated content" };

        updated.UserId.Should().Be("user-alice", "record with syntax should preserve userId");
    }

    [TestMethod]
    public void UserSession_RecordWith_PreservesUserId()
    {
        var original = new UserSession
        {
            SessionId = "session-1",
            UserId = "user-bob",
            DocumentIds = ["doc-1"],
            Timestamp = DateTimeOffset.UtcNow
        };

        var updated = original with { DocumentIds = ["doc-1", "doc-2"] };

        updated.UserId.Should().Be("user-bob", "record with syntax should preserve userId");
    }

    [TestMethod]
    public void Suggestion_RecordWith_PreservesUserId()
    {
        var original = new Suggestion
        {
            Id = "sug-1",
            DocumentId = "doc-1",
            UserId = "user-charlie",
            TextRange = new TextRange { Start = 0, End = 10 },
            Rationale = "Original rationale",
            ProposedChange = "Original change",
            Status = SuggestionStatus.Pending
        };

        var updated = original with { Status = SuggestionStatus.Accepted };

        updated.UserId.Should().Be("user-charlie", "record with syntax should preserve userId");
    }
}
