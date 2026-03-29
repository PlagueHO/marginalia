using System.Text.Json;
using FluentAssertions;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Domain;

[TestClass]
[TestCategory("Unit")]
public sealed class SuggestionStatusTests
{
    [TestMethod]
    public void Enum_ContainsExpectedValues()
    {
        var values = Enum.GetValues<SuggestionStatus>();

        values.Should().Contain(SuggestionStatus.Pending);
        values.Should().Contain(SuggestionStatus.Accepted);
        values.Should().Contain(SuggestionStatus.Rejected);
        values.Should().Contain(SuggestionStatus.Modified);
    }

    [TestMethod]
    public void Enum_HasExactlyFourValues()
    {
        var values = Enum.GetValues<SuggestionStatus>();
        values.Should().HaveCount(4);
    }

    [TestMethod]
    public void Serialization_Pending_SerializesAsString()
    {
        var json = JsonSerializer.Serialize(SuggestionStatus.Pending);
        json.Should().Be("\"Pending\"");
    }

    [TestMethod]
    public void Serialization_Accepted_SerializesAsString()
    {
        var json = JsonSerializer.Serialize(SuggestionStatus.Accepted);
        json.Should().Be("\"Accepted\"");
    }

    [TestMethod]
    public void Serialization_Rejected_SerializesAsString()
    {
        var json = JsonSerializer.Serialize(SuggestionStatus.Rejected);
        json.Should().Be("\"Rejected\"");
    }

    [TestMethod]
    public void Serialization_Modified_SerializesAsString()
    {
        var json = JsonSerializer.Serialize(SuggestionStatus.Modified);
        json.Should().Be("\"Modified\"");
    }

    [TestMethod]
    public void Deserialization_FromString_ReconstructsEnum()
    {
        var pending = JsonSerializer.Deserialize<SuggestionStatus>("\"Pending\"");
        var accepted = JsonSerializer.Deserialize<SuggestionStatus>("\"Accepted\"");
        var rejected = JsonSerializer.Deserialize<SuggestionStatus>("\"Rejected\"");
        var modified = JsonSerializer.Deserialize<SuggestionStatus>("\"Modified\"");

        pending.Should().Be(SuggestionStatus.Pending);
        accepted.Should().Be(SuggestionStatus.Accepted);
        rejected.Should().Be(SuggestionStatus.Rejected);
        modified.Should().Be(SuggestionStatus.Modified);
    }

    [TestMethod]
    public void Deserialization_InvalidString_ThrowsJsonException()
    {
        var act = () => JsonSerializer.Deserialize<SuggestionStatus>("\"InvalidStatus\"");
        act.Should().Throw<JsonException>();
    }

    [TestMethod]
    public void ValidTransitions_PendingCanTransitionToAccepted()
    {
        // Documents the valid workflow: new suggestions start Pending
        SuggestionStatus initial = SuggestionStatus.Pending;
        SuggestionStatus target = SuggestionStatus.Accepted;

        initial.Should().NotBe(target);
        target.Should().Be(SuggestionStatus.Accepted);
    }

    [TestMethod]
    public void ValidTransitions_PendingCanTransitionToRejected()
    {
        SuggestionStatus initial = SuggestionStatus.Pending;
        SuggestionStatus target = SuggestionStatus.Rejected;

        initial.Should().NotBe(target);
        target.Should().Be(SuggestionStatus.Rejected);
    }

    [TestMethod]
    public void ValidTransitions_PendingCanTransitionToModified()
    {
        SuggestionStatus initial = SuggestionStatus.Pending;
        SuggestionStatus target = SuggestionStatus.Modified;

        initial.Should().NotBe(target);
        target.Should().Be(SuggestionStatus.Modified);
    }
}
