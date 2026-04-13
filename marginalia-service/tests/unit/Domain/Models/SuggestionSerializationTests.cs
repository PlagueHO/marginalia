using System.Text.Json;
using FluentAssertions;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Domain.Models;

[TestClass]
[TestCategory("Unit")]
public sealed class SuggestionSerializationTests
{
    [TestMethod]
    public void Deserialize_WhenLegacyPayloadOmitsParagraphId_ReturnsSuggestionWithEmptyParagraphId()
    {
        const string json = """
            {
              "id": "sug-1",
              "userId": "user-1",
              "documentId": "doc-1",
              "rationale": "Legacy suggestion without a paragraph reference.",
              "proposedChange": "Expanded version",
              "status": 0
            }
            """;

        var suggestion = JsonSerializer.Deserialize<Suggestion>(json);

        suggestion.Should().NotBeNull();
        suggestion!.ParagraphId.Should().BeEmpty();
        suggestion.DocumentId.Should().Be("doc-1");
        suggestion.Rationale.Should().Be("Legacy suggestion without a paragraph reference.");
    }
}