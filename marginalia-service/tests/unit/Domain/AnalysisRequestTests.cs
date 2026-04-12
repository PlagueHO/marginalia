using System.Text.Json;
using FluentAssertions;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Domain;

[TestClass]
[TestCategory("Unit")]
public sealed class AnalysisRequestTests
{
    [TestMethod]
    public void AnalysisRequest_Deserialization_WithCurrentFieldNames_BindsEffectiveGuidance()
    {
        const string json = """
        {
            "documentId": "doc-1",
            "userInstructions": "Focus on pacing and transitions",
            "toneGuidance": "conversational"
        }
        """;

        var request = JsonSerializer.Deserialize<AnalysisRequest>(json);

        request.Should().NotBeNull();
        request!.EffectiveUserInstructions.Should().Be("Focus on pacing and transitions");
        request.EffectiveToneGuidance.Should().Be("conversational");
    }

    [TestMethod]
    public void AnalysisRequest_Deserialization_WithLegacyFieldNames_BindsEffectiveGuidance()
    {
        const string json = """
        {
            "documentId": "doc-1",
            "userGuidance": "Prioritize clarity",
            "tone": "professional"
        }
        """;

        var request = JsonSerializer.Deserialize<AnalysisRequest>(json);

        request.Should().NotBeNull();
        request!.EffectiveUserInstructions.Should().Be("Prioritize clarity");
        request.EffectiveToneGuidance.Should().Be("professional");
    }

    [TestMethod]
    public void AnalysisRequest_Deserialization_WhenBothFormsProvided_PrefersCurrentFields()
    {
        const string json = """
        {
            "documentId": "doc-1",
            "userInstructions": "Use current instructions",
            "toneGuidance": "academic",
            "userGuidance": "Use legacy instructions",
            "tone": "narrative"
        }
        """;

        var request = JsonSerializer.Deserialize<AnalysisRequest>(json);

        request.Should().NotBeNull();
        request!.EffectiveUserInstructions.Should().Be("Use current instructions");
        request.EffectiveToneGuidance.Should().Be("academic");
    }
}
