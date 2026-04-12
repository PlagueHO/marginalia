using System.Text.Json;
using FluentAssertions;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Domain;

/// <summary>
/// Tests for the DocumentListResponse wrapper returned by GET /api/documents.
/// Verifies the response wraps documents in an object (not a bare array)
/// following REST best practices for future pagination support.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public sealed class DocumentListResponseTests
{
    [TestMethod]
    public void Constructor_WithEmptyDocuments_CreatesResponse()
    {
        var response = new DocumentListResponse
        {
            Documents = []
        };

        response.Documents.Should().BeEmpty();
    }

    [TestMethod]
    public void Constructor_WithMultipleDocuments_StoresAll()
    {
        var now = DateTimeOffset.UtcNow;
        var docs = new List<DocumentSummary>
        {
            new()
            {
                Id = "doc-1",
                Title = "First Draft",
                Filename = "first.docx",
                Source = DocumentSource.Local,
                Status = DocumentStatus.Draft,
                CreatedAt = now,
                UpdatedAt = now,
                SuggestionCount = 0,
                ParagraphCount = 1
            },
            new()
            {
                Id = "doc-2",
                Title = "Second Draft",
                Filename = "second.docx",
                Source = DocumentSource.Local,
                Status = DocumentStatus.Analyzed,
                CreatedAt = now.AddHours(-2),
                UpdatedAt = now,
                SuggestionCount = 5,
                ParagraphCount = 3
            }
        };

        var response = new DocumentListResponse { Documents = docs };

        response.Documents.Should().HaveCount(2);
        response.Documents[0].Id.Should().Be("doc-1");
        response.Documents[1].Id.Should().Be("doc-2");
    }

    [TestMethod]
    public void Serialization_WrapsInDocumentsProperty()
    {
        var response = new DocumentListResponse
        {
            Documents = new List<DocumentSummary>
            {
                new()
                {
                    Id = "doc-1",
                    Title = "Test",
                    Filename = "test.docx",
                    Source = DocumentSource.Local,
                    Status = DocumentStatus.Draft,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    SuggestionCount = 0,
                    ParagraphCount = 0
                }
            }
        };

        var json = JsonSerializer.Serialize(response);

        json.Should().Contain("\"documents\":");
        json.Should().NotStartWith("[", "response should be an object, not a bare array");
    }

    [TestMethod]
    public void Serialization_EmptyList_ProducesEmptyArray()
    {
        var response = new DocumentListResponse { Documents = [] };

        var json = JsonSerializer.Serialize(response);

        json.Should().Contain("\"documents\":[]");
    }

    [TestMethod]
    public void Deserialization_FromJson_ReconstructsResponse()
    {
        const string json = """
        {
            "documents": [
                {
                    "id": "doc-1",
                    "title": "Chapter 1",
                    "filename": "ch1.docx",
                    "source": "Local",
                    "status": "Draft",
                    "createdAt": "2026-03-29T10:15:00+00:00",
                    "updatedAt": "2026-03-29T10:15:00+00:00",
                    "suggestionCount": 0,
                    "paragraphCount": 1
                },
                {
                    "id": "doc-2",
                    "title": "Chapter 2",
                    "filename": "ch2.docx",
                    "source": "Local",
                    "status": "Analyzed",
                    "createdAt": "2026-03-28T09:00:00+00:00",
                    "updatedAt": "2026-03-29T14:30:00+00:00",
                    "suggestionCount": 8,
                    "paragraphCount": 5
                }
            ]
        }
        """;

        var response = JsonSerializer.Deserialize<DocumentListResponse>(json);

        response.Should().NotBeNull();
        response!.Documents.Should().HaveCount(2);
        response.Documents[0].Title.Should().Be("Chapter 1");
        response.Documents[1].SuggestionCount.Should().Be(8);
    }

    [TestMethod]
    public void Deserialization_EmptyDocumentsArray_IsValid()
    {
        const string json = """{ "documents": [] }""";

        var response = JsonSerializer.Deserialize<DocumentListResponse>(json);

        response.Should().NotBeNull();
        response!.Documents.Should().BeEmpty();
    }
}
