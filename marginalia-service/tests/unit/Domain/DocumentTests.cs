using System.Text.Json;
using FluentAssertions;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Domain;

[TestClass]
[TestCategory("Unit")]
public sealed class DocumentTests
{
    [TestMethod]
    public void Constructor_WithRequiredFields_CreatesDocument()
    {
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "chapter1.docx",
            Source = DocumentSource.Local,
            Content = "Once upon a time..."
        };

        doc.Id.Should().Be("doc-1");
        doc.Filename.Should().Be("chapter1.docx");
        doc.Source.Should().Be(DocumentSource.Local);
        doc.Content.Should().Be("Once upon a time...");
    }

    [TestMethod]
    public void Suggestions_DefaultsToEmptyList_WhenNotProvided()
    {
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Content = "content"
        };

        doc.Suggestions.Should().BeEmpty();
    }

    [TestMethod]
    public void Suggestions_WhenProvided_AreAccessible()
    {
        var suggestion = new Suggestion
        {
            Id = "sug-1",
            DocumentId = "doc-1",
            TextRange = new TextRange { Start = 0, End = 10 },
            Rationale = "Too compressed",
            ProposedChange = "Expanded text",
            Status = SuggestionStatus.Pending
        };

        var doc = new Document
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Content = "content",
            Suggestions = [suggestion]
        };

        doc.Suggestions.Should().HaveCount(1);
        doc.Suggestions[0].Id.Should().Be("sug-1");
    }

    [TestMethod]
    public void Serialization_ProducesCamelCasePropertyNames()
    {
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Content = "content"
        };

        var json = JsonSerializer.Serialize(doc);

        json.Should().Contain("\"id\":");
        json.Should().Contain("\"filename\":");
        json.Should().Contain("\"source\":");
        json.Should().Contain("\"content\":");
        json.Should().Contain("\"suggestions\":");
    }

    [TestMethod]
    public void Deserialization_FromCamelCaseJson_ReconstructsDocument()
    {
        const string json = """
        {
            "id": "doc-1",
            "filename": "test.docx",
            "source": "Local",
            "content": "hello world",
            "suggestions": []
        }
        """;

        var doc = JsonSerializer.Deserialize<Document>(json);

        doc.Should().NotBeNull();
        doc!.Id.Should().Be("doc-1");
        doc.Filename.Should().Be("test.docx");
        doc.Source.Should().Be(DocumentSource.Local);
        doc.Content.Should().Be("hello world");
        doc.Suggestions.Should().BeEmpty();
    }

    [TestMethod]
    public void Source_GoogleDocs_SerializesAsString()
    {
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "imported.docx",
            Source = DocumentSource.GoogleDocs,
            Content = "content"
        };

        var json = JsonSerializer.Serialize(doc);
        json.Should().Contain("\"GoogleDocs\"");
    }

    [TestMethod]
    public void Record_Equality_MatchesOnAllProperties()
    {
        var doc1 = new Document
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Content = "content"
        };

        var doc2 = new Document
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Content = "content"
        };

        doc1.Should().Be(doc2);
    }

    [TestMethod]
    public void Record_With_CreatesModifiedCopy_WithoutMutatingOriginal()
    {
        var original = new Document
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Content = "original content"
        };

        var modified = original with { Content = "revised content" };

        modified.Content.Should().Be("revised content");
        modified.Id.Should().Be("doc-1");
        original.Content.Should().Be("original content");
    }

    [TestMethod]
    public void Content_EmptyString_IsValid()
    {
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "empty.docx",
            Source = DocumentSource.Local,
            Content = ""
        };

        doc.Content.Should().BeEmpty();
    }

    [TestMethod]
    public void Content_LargeText_IsHandled()
    {
        var largeContent = new string('A', 100_000);
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "long-manuscript.docx",
            Source = DocumentSource.Local,
            Content = largeContent
        };

        doc.Content.Should().HaveLength(100_000);
    }
}
