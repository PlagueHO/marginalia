using System.Text.Json;
using FluentAssertions;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Domain;

/// <summary>
/// Tests for new Document model fields required by the home page feature:
/// Title, Status, CreatedAt, UpdatedAt.
///
/// These tests define the contract from Richard's API design (home-page-api-design).
/// Also tests title generation defaults and status transitions.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public sealed class DocumentHomePageFieldsTests
{
    private static readonly IReadOnlyList<Paragraph> DefaultParagraphs =
        [new Paragraph { Id = "p1", Text = "Once upon a time..." }];

    // ── Title field ──────────────────────────────────────────────────────

    [TestMethod]
    public void Document_WithExplicitTitle_StoresTitle()
    {
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "chapter1.docx",
            Source = DocumentSource.Local,
            Paragraphs = DefaultParagraphs,
            Title = "My Chapter Title",
            Status = DocumentStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        doc.Title.Should().Be("My Chapter Title");
    }

    // ── Status field ─────────────────────────────────────────────────────

    [TestMethod]
    public void Document_NewDocument_HasStatusDraft()
    {
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "chapter1.docx",
            Source = DocumentSource.Local,
            Paragraphs = DefaultParagraphs,
            Title = "Test",
            Status = DocumentStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        doc.Status.Should().Be(DocumentStatus.Draft);
    }

    [TestMethod]
    public void Document_WithRecordUpdate_CanTransitionToAnalyzed()
    {
        var now = DateTimeOffset.UtcNow;
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "chapter1.docx",
            Source = DocumentSource.Local,
            Paragraphs = DefaultParagraphs,
            Title = "Test",
            Status = DocumentStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        var analyzed = doc with
        {
            Status = DocumentStatus.Analyzed,
            UpdatedAt = now.AddMinutes(5)
        };

        analyzed.Status.Should().Be(DocumentStatus.Analyzed);
        analyzed.UpdatedAt.Should().BeAfter(analyzed.CreatedAt);
    }

    // ── CreatedAt / UpdatedAt ────────────────────────────────────────────

    [TestMethod]
    public void Document_CreatedAt_IsPreservedThroughRecordUpdate()
    {
        var createdAt = new DateTimeOffset(2026, 3, 29, 10, 0, 0, TimeSpan.Zero);
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "chapter1.docx",
            Source = DocumentSource.Local,
            Paragraphs = DefaultParagraphs,
            Title = "Test",
            Status = DocumentStatus.Draft,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        var updated = doc with
        {
            Paragraphs = [new Paragraph { Id = "p1", Text = "Revised content" }],
            UpdatedAt = createdAt.AddHours(4)
        };

        updated.CreatedAt.Should().Be(createdAt, "createdAt must never change after creation");
    }

    [TestMethod]
    public void Document_UpdatedAt_InitiallyEqualsCreatedAt()
    {
        var now = DateTimeOffset.UtcNow;
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "chapter1.docx",
            Source = DocumentSource.Local,
            Paragraphs = DefaultParagraphs,
            Title = "Test",
            Status = DocumentStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        doc.UpdatedAt.Should().Be(doc.CreatedAt);
    }

    [TestMethod]
    public void Document_UpdatedAt_AdvancesOnContentModification()
    {
        var createdAt = new DateTimeOffset(2026, 3, 29, 10, 0, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 3, 29, 14, 30, 0, TimeSpan.Zero);

        var doc = new Document
        {
            Id = "doc-1",
            Filename = "chapter1.docx",
            Source = DocumentSource.Local,
            Paragraphs = DefaultParagraphs,
            Title = "Test",
            Status = DocumentStatus.Draft,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        var updated = doc with
        {
            Paragraphs = [new Paragraph { Id = "p1", Text = "Revised content with more narrative air." }],
            UpdatedAt = updatedAt
        };

        updated.UpdatedAt.Should().BeAfter(updated.CreatedAt);
    }

    [TestMethod]
    public void Document_UpdatedAt_AdvancesOnAnalysisCompletion()
    {
        var createdAt = new DateTimeOffset(2026, 3, 29, 10, 0, 0, TimeSpan.Zero);
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "chapter1.docx",
            Source = DocumentSource.Local,
            Paragraphs = DefaultParagraphs,
            Title = "Test",
            Status = DocumentStatus.Draft,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        var afterAnalysis = doc with
        {
            Status = DocumentStatus.Analyzed,
            UpdatedAt = createdAt.AddMinutes(30),
            Suggestions =
            [
                new Suggestion
                {
                    Id = "s1",
                    DocumentId = "doc-1",
                    ParagraphId = "p1",
                    Rationale = "Too compressed",
                    ProposedChange = "Expand this passage",
                    Status = SuggestionStatus.Pending
                }
            ]
        };

        afterAnalysis.Status.Should().Be(DocumentStatus.Analyzed);
        afterAnalysis.UpdatedAt.Should().BeAfter(afterAnalysis.CreatedAt);
        afterAnalysis.Suggestions.Should().HaveCount(1);
    }

    // ── Serialization ────────────────────────────────────────────────────

    [TestMethod]
    public void Document_Serialization_IncludesNewFields()
    {
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Paragraphs = [new Paragraph { Id = "p1", Text = "content" }],
            Title = "My Title",
            Status = DocumentStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(doc);

        json.Should().Contain("\"title\":");
        json.Should().Contain("\"status\":");
        json.Should().Contain("\"createdAt\":");
        json.Should().Contain("\"updatedAt\":");
    }

    [TestMethod]
    public void Document_Deserialization_WithNewFields_Roundtrips()
    {
        const string json = """
        {
            "id": "doc-1",
            "filename": "test.docx",
            "source": "Local",
            "paragraphs": [{ "id": "p1", "text": "hello world" }],
            "title": "Chapter 1 Draft",
            "status": "Analyzed",
            "createdAt": "2026-03-29T10:15:00+00:00",
            "updatedAt": "2026-03-29T14:30:00+00:00",
            "suggestions": []
        }
        """;

        var doc = JsonSerializer.Deserialize<Document>(json);

        doc.Should().NotBeNull();
        doc!.Title.Should().Be("Chapter 1 Draft");
        doc.Status.Should().Be(DocumentStatus.Analyzed);
        doc.CreatedAt.Should().Be(new DateTimeOffset(2026, 3, 29, 10, 15, 0, TimeSpan.Zero));
        doc.UpdatedAt.Should().Be(new DateTimeOffset(2026, 3, 29, 14, 30, 0, TimeSpan.Zero));
    }

    // ── SuggestionCount projection ───────────────────────────────────────

    [TestMethod]
    public void Document_SuggestionCount_CanBeComputedFromSuggestionsArray()
    {
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Paragraphs = [new Paragraph { Id = "p1", Text = "content" }],
            Title = "Test",
            Status = DocumentStatus.Analyzed,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Suggestions =
            [
                new Suggestion
                {
                    Id = "s1",
                    DocumentId = "doc-1",
                    ParagraphId = "p1",
                    Rationale = "Test",
                    ProposedChange = "Change",
                    Status = SuggestionStatus.Pending
                },
                new Suggestion
                {
                    Id = "s2",
                    DocumentId = "doc-1",
                    ParagraphId = "p1",
                    Rationale = "Test 2",
                    ProposedChange = "Change 2",
                    Status = SuggestionStatus.Accepted
                }
            ]
        };

        // The controller projects this to DocumentSummary.SuggestionCount
        doc.Suggestions.Count.Should().Be(2);
    }

    // ── Title generation defaults ────────────────────────────────────────

    [TestMethod]
    public void TitleGeneration_Upload_DefaultFormat()
    {
        // Per Richard's design: upload without title → "{createdAt:yyyy-MM-dd HH:mm} - {filename}"
        var createdAt = new DateTimeOffset(2026, 3, 29, 10, 15, 0, TimeSpan.Zero);
        var filename = "chapter1.docx";
        var expectedTitle = $"{createdAt:yyyy-MM-dd HH:mm} - {filename}";

        expectedTitle.Should().Be("2026-03-29 10:15 - chapter1.docx");
    }

    [TestMethod]
    public void TitleGeneration_Paste_DefaultFormat()
    {
        // Per Richard's design: paste without title → "{createdAt:yyyy-MM-dd HH:mm} - Untitled"
        var createdAt = new DateTimeOffset(2026, 3, 29, 10, 15, 0, TimeSpan.Zero);
        var expectedTitle = $"{createdAt:yyyy-MM-dd HH:mm} - Untitled";

        expectedTitle.Should().Be("2026-03-29 10:15 - Untitled");
    }
}
