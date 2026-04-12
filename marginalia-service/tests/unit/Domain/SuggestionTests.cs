using System.Text.Json;
using FluentAssertions;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Domain;

[TestClass]
[TestCategory("Unit")]
public sealed class SuggestionTests
{
    private static Suggestion CreateSuggestion(
        SuggestionStatus status = SuggestionStatus.Pending,
        string? userSteeringInput = null) => new()
        {
            Id = "sug-1",
            DocumentId = "doc-1",
            ParagraphId = "para-1",
            Rationale = "Narrative is compressed here",
            ProposedChange = "Consider expanding with sensory detail",
            Status = status,
            UserSteeringInput = userSteeringInput
        };

    [TestMethod]
    public void Constructor_WithRequiredFields_CreatesSuggestion()
    {
        var suggestion = CreateSuggestion();

        suggestion.Id.Should().Be("sug-1");
        suggestion.DocumentId.Should().Be("doc-1");
        suggestion.Rationale.Should().Contain("compressed");
        suggestion.ProposedChange.Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public void Status_Pending_IsDefaultForNewSuggestions()
    {
        var suggestion = CreateSuggestion(SuggestionStatus.Pending);
        suggestion.Status.Should().Be(SuggestionStatus.Pending);
    }

    [TestMethod]
    public void Status_Accepted_RepresentsApprovedSuggestion()
    {
        var suggestion = CreateSuggestion(SuggestionStatus.Accepted);
        suggestion.Status.Should().Be(SuggestionStatus.Accepted);
    }

    [TestMethod]
    public void Status_Rejected_RepresentsDeclinedSuggestion()
    {
        var suggestion = CreateSuggestion(SuggestionStatus.Rejected);
        suggestion.Status.Should().Be(SuggestionStatus.Rejected);
    }

    [TestMethod]
    public void Status_Modified_RepresentsUserEditedSuggestion()
    {
        var suggestion = CreateSuggestion(SuggestionStatus.Modified);
        suggestion.Status.Should().Be(SuggestionStatus.Modified);
    }

    [TestMethod]
    public void StatusTransition_PendingToAccepted_ViaRecordWith()
    {
        var pending = CreateSuggestion(SuggestionStatus.Pending);
        var accepted = pending with { Status = SuggestionStatus.Accepted };

        accepted.Status.Should().Be(SuggestionStatus.Accepted);
        pending.Status.Should().Be(SuggestionStatus.Pending);
    }

    [TestMethod]
    public void StatusTransition_PendingToRejected_ViaRecordWith()
    {
        var pending = CreateSuggestion(SuggestionStatus.Pending);
        var rejected = pending with { Status = SuggestionStatus.Rejected };

        rejected.Status.Should().Be(SuggestionStatus.Rejected);
    }

    [TestMethod]
    public void StatusTransition_PendingToModified_ViaRecordWith()
    {
        var pending = CreateSuggestion(SuggestionStatus.Pending);
        var modified = pending with
        {
            Status = SuggestionStatus.Modified,
            ProposedChange = "User-edited expansion with more color"
        };

        modified.Status.Should().Be(SuggestionStatus.Modified);
        modified.ProposedChange.Should().Contain("User-edited");
    }

    [TestMethod]
    public void UserSteeringInput_IsNull_WhenNotProvided()
    {
        var suggestion = CreateSuggestion();
        suggestion.UserSteeringInput.Should().BeNull();
    }

    [TestMethod]
    public void UserSteeringInput_IsPreserved_WhenProvided()
    {
        var suggestion = CreateSuggestion(
            userSteeringInput: "Make it more narrative, less academic");

        suggestion.UserSteeringInput.Should().Be("Make it more narrative, less academic");
    }

    [TestMethod]
    public void Serialization_StatusSerializesAsString()
    {
        var suggestion = CreateSuggestion(SuggestionStatus.Pending);
        var json = JsonSerializer.Serialize(suggestion);

        json.Should().Contain("\"Pending\"");
        json.Should().NotContain("\"0\"");
    }

    [TestMethod]
    public void Serialization_AllPropertiesUseCamelCase()
    {
        var suggestion = CreateSuggestion(
            userSteeringInput: "More detail please");
        var json = JsonSerializer.Serialize(suggestion);

        json.Should().Contain("\"id\":");
        json.Should().Contain("\"documentId\":");
        json.Should().Contain("\"paragraphId\":");
        json.Should().Contain("\"rationale\":");
        json.Should().Contain("\"proposedChange\":");
        json.Should().Contain("\"status\":");
        json.Should().Contain("\"userSteeringInput\":");
    }

    [TestMethod]
    public void Deserialization_FromCamelCaseJson_ReconstructsSuggestion()
    {
        const string json = """
        {
            "id": "sug-42",
            "documentId": "doc-7",
            "paragraphId": "para-42",
            "rationale": "Style inconsistency",
            "proposedChange": "Revised passage",
            "status": "Rejected",
            "userSteeringInput": "Keep my voice"
        }
        """;

        var suggestion = JsonSerializer.Deserialize<Suggestion>(json);

        suggestion.Should().NotBeNull();
        suggestion!.Id.Should().Be("sug-42");
        suggestion.Status.Should().Be(SuggestionStatus.Rejected);
        suggestion.UserSteeringInput.Should().Be("Keep my voice");
        suggestion.ParagraphId.Should().Be("para-42");
    }

    [TestMethod]
    public void Deserialization_WithNullUserSteeringInput_Succeeds()
    {
        const string json = """
        {
            "id": "sug-1",
            "documentId": "doc-1",
            "paragraphId": "para-1",
            "rationale": "Reason",
            "proposedChange": "Change",
            "status": "Pending"
        }
        """;

        var suggestion = JsonSerializer.Deserialize<Suggestion>(json);
        suggestion.Should().NotBeNull();
        suggestion!.UserSteeringInput.Should().BeNull();
    }
}
