using System.Text.Json;
using FluentAssertions;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Domain;

/// <summary>
/// Tests for the optional Title field on PasteDocumentRequest.
///
/// These tests will NOT compile until Gilfoyle adds the Title property
/// to PasteDocumentRequest in Domain/Models/PasteDocumentRequest.cs.
///
/// Per Richard's design: title is optional. When omitted, the controller
/// generates a default: "{createdAt:yyyy-MM-dd HH:mm} - Untitled".
/// </summary>
[TestClass]
[TestCategory("Unit")]
public sealed class PasteDocumentRequestTitleTests
{
    [TestMethod]
    public void PasteDocumentRequest_WithTitle_StoresTitle()
    {
        var request = new PasteDocumentRequest
        {
            Content = "The morning light filtered through the study window.",
            Title = "My Research Notes"
        };

        request.Title.Should().Be("My Research Notes");
    }

    [TestMethod]
    public void PasteDocumentRequest_WithoutTitle_TitleIsNull()
    {
        var request = new PasteDocumentRequest
        {
            Content = "Some manuscript content."
        };

        request.Title.Should().BeNull();
    }

    [TestMethod]
    public void PasteDocumentRequest_Serialization_IncludesTitleWhenProvided()
    {
        var request = new PasteDocumentRequest
        {
            Content = "Text content",
            Title = "Chapter Notes"
        };

        var json = JsonSerializer.Serialize(request);

        json.Should().Contain("\"title\":");
        json.Should().Contain("\"Chapter Notes\"");
    }

    [TestMethod]
    public void PasteDocumentRequest_Deserialization_WithTitle_Roundtrips()
    {
        const string json = """
        {
            "content": "The morning light...",
            "filename": "notes.txt",
            "title": "My Notes"
        }
        """;

        var request = JsonSerializer.Deserialize<PasteDocumentRequest>(json);

        request.Should().NotBeNull();
        request!.Content.Should().Be("The morning light...");
        request.Filename.Should().Be("notes.txt");
        request.Title.Should().Be("My Notes");
    }

    [TestMethod]
    public void PasteDocumentRequest_Deserialization_WithoutTitle_TitleIsNull()
    {
        const string json = """
        {
            "content": "Some text",
            "filename": "notes.txt"
        }
        """;

        var request = JsonSerializer.Deserialize<PasteDocumentRequest>(json);

        request.Should().NotBeNull();
        request!.Title.Should().BeNull();
    }
}
