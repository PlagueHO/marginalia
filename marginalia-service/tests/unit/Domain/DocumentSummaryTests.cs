using System.Text.Json;
using FluentAssertions;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Domain;

/// <summary>
/// Tests for the DocumentSummary listing DTO.
/// Verifies the DTO carries the right fields for the home page document list,
/// and that it excludes content and suggestions (lightweight projection).
/// </summary>
[TestClass]
[TestCategory("Unit")]
public sealed class DocumentSummaryTests
{
    private static DocumentSummary CreateSummary(
        string id = "doc-1",
        string title = "Test Document",
        string filename = "test.docx",
        DocumentSource source = DocumentSource.Local,
        DocumentStatus status = DocumentStatus.Draft,
        int suggestionCount = 0,
        int paragraphCount = 0) => new()
        {
            Id = id,
            Title = title,
            Filename = filename,
            Source = source,
            Status = status,
            CreatedAt = new DateTimeOffset(2026, 3, 29, 10, 15, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 3, 29, 14, 30, 0, TimeSpan.Zero),
            SuggestionCount = suggestionCount,
            ParagraphCount = paragraphCount
        };

    [TestMethod]
    public void Constructor_WithRequiredFields_CreatesSummary()
    {
        var summary = CreateSummary();

        summary.Id.Should().Be("doc-1");
        summary.Title.Should().Be("Test Document");
        summary.Filename.Should().Be("test.docx");
        summary.Source.Should().Be(DocumentSource.Local);
        summary.Status.Should().Be(DocumentStatus.Draft);
        summary.SuggestionCount.Should().Be(0);
    }

    [TestMethod]
    public void SuggestionCount_ReflectsProvidedValue()
    {
        var summary = CreateSummary(suggestionCount: 12);
        summary.SuggestionCount.Should().Be(12);
    }

    [TestMethod]
    public void Status_CanBeDraft()
    {
        var summary = CreateSummary(status: DocumentStatus.Draft);
        summary.Status.Should().Be(DocumentStatus.Draft);
    }

    [TestMethod]
    public void Status_CanBeAnalyzed()
    {
        var summary = CreateSummary(status: DocumentStatus.Analyzed);
        summary.Status.Should().Be(DocumentStatus.Analyzed);
    }

    [TestMethod]
    public void CreatedAt_IsStoredCorrectly()
    {
        var summary = CreateSummary();
        summary.CreatedAt.Should().Be(new DateTimeOffset(2026, 3, 29, 10, 15, 0, TimeSpan.Zero));
    }

    [TestMethod]
    public void UpdatedAt_IsStoredCorrectly()
    {
        var summary = CreateSummary();
        summary.UpdatedAt.Should().Be(new DateTimeOffset(2026, 3, 29, 14, 30, 0, TimeSpan.Zero));
    }

    [TestMethod]
    public void Serialization_ProducesCamelCasePropertyNames()
    {
        var summary = CreateSummary();
        var json = JsonSerializer.Serialize(summary);

        json.Should().Contain("\"id\":");
        json.Should().Contain("\"title\":");
        json.Should().Contain("\"filename\":");
        json.Should().Contain("\"source\":");
        json.Should().Contain("\"status\":");
        json.Should().Contain("\"createdAt\":");
        json.Should().Contain("\"updatedAt\":");
        json.Should().Contain("\"suggestionCount\":");
        json.Should().Contain("\"paragraphCount\":");
    }

    [TestMethod]
    public void Serialization_DoesNotIncludeContentField()
    {
        var summary = CreateSummary();
        var json = JsonSerializer.Serialize(summary);

        json.Should().NotContain("\"content\":");
    }

    [TestMethod]
    public void Serialization_DoesNotIncludeSuggestionsArray()
    {
        var summary = CreateSummary();
        var json = JsonSerializer.Serialize(summary);

        json.Should().NotContain("\"suggestions\":");
    }

    [TestMethod]
    public void Serialization_StatusIsString_NotNumeric()
    {
        var summary = CreateSummary(status: DocumentStatus.Analyzed);
        var json = JsonSerializer.Serialize(summary);

        json.Should().Contain("\"Analyzed\"");
        json.Should().NotContain("\"status\":1");
    }

    [TestMethod]
    public void Deserialization_FromJson_ReconstructsSummary()
    {
        const string json = """
        {
            "id": "doc-1",
            "title": "Chapter 1 Draft",
            "filename": "chapter1.docx",
            "source": "Local",
            "status": "Analyzed",
            "createdAt": "2026-03-29T10:15:00+00:00",
            "updatedAt": "2026-03-29T14:30:00+00:00",
            "suggestionCount": 12,
            "paragraphCount": 5
        }
        """;

        var summary = JsonSerializer.Deserialize<DocumentSummary>(json);

        summary.Should().NotBeNull();
        summary!.Id.Should().Be("doc-1");
        summary.Title.Should().Be("Chapter 1 Draft");
        summary.Filename.Should().Be("chapter1.docx");
        summary.Source.Should().Be(DocumentSource.Local);
        summary.Status.Should().Be(DocumentStatus.Analyzed);
        summary.SuggestionCount.Should().Be(12);
    }

    [TestMethod]
    public void Deserialization_ZeroSuggestionCount_IsValid()
    {
        const string json = """
        {
            "id": "doc-2",
            "title": "New Draft",
            "filename": "draft.docx",
            "source": "Local",
            "status": "Draft",
            "createdAt": "2026-03-28T09:00:00+00:00",
            "updatedAt": "2026-03-28T09:00:00+00:00",
            "suggestionCount": 0,
            "paragraphCount": 0
        }
        """;

        var summary = JsonSerializer.Deserialize<DocumentSummary>(json);

        summary.Should().NotBeNull();
        summary!.SuggestionCount.Should().Be(0);
        summary.Status.Should().Be(DocumentStatus.Draft);
    }
}
