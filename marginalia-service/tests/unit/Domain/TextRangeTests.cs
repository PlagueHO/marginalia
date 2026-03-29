using System.Text.Json;
using FluentAssertions;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Domain;

[TestClass]
[TestCategory("Unit")]
public sealed class TextRangeTests
{
    [TestMethod]
    public void Constructor_ValidRange_CreatesTextRange()
    {
        var range = new TextRange { Start = 0, End = 100 };

        range.Start.Should().Be(0);
        range.End.Should().Be(100);
    }

    [TestMethod]
    public void Constructor_SingleCharacterRange_IsValid()
    {
        var range = new TextRange { Start = 5, End = 6 };

        range.Start.Should().Be(5);
        range.End.Should().Be(6);
    }

    [TestMethod]
    public void Constructor_StartEqualsEnd_RepresentsEmptyRange()
    {
        // Edge case: zero-length range (cursor position).
        // Domain model allows this; validation should be in the service layer.
        var range = new TextRange { Start = 10, End = 10 };

        range.Start.Should().Be(range.End);
    }

    [TestMethod]
    public void Constructor_StartGreaterThanEnd_IsNotPreventedByRecord()
    {
        // Edge case: record doesn't enforce ordering.
        // This documents that validation must live elsewhere.
        var range = new TextRange { Start = 50, End = 10 };

        range.Start.Should().BeGreaterThan(range.End);
    }

    [TestMethod]
    public void Constructor_NegativeStart_IsNotPreventedByRecord()
    {
        // Edge case: record doesn't enforce non-negative values.
        var range = new TextRange { Start = -1, End = 10 };

        range.Start.Should().BeNegative();
    }

    [TestMethod]
    public void Constructor_ZeroStart_IsValid()
    {
        var range = new TextRange { Start = 0, End = 1 };
        range.Start.Should().Be(0);
    }

    [TestMethod]
    public void Constructor_LargeValues_AreHandled()
    {
        // A 10-page manuscript could have 30,000+ characters
        var range = new TextRange { Start = 0, End = 30_000 };

        range.End.Should().Be(30_000);
    }

    [TestMethod]
    public void Record_Equality_MatchesOnStartAndEnd()
    {
        var range1 = new TextRange { Start = 10, End = 20 };
        var range2 = new TextRange { Start = 10, End = 20 };

        range1.Should().Be(range2);
    }

    [TestMethod]
    public void Record_Inequality_DifferentStart()
    {
        var range1 = new TextRange { Start = 10, End = 20 };
        var range2 = new TextRange { Start = 11, End = 20 };

        range1.Should().NotBe(range2);
    }

    [TestMethod]
    public void Serialization_ProducesCamelCase()
    {
        var range = new TextRange { Start = 5, End = 15 };
        var json = JsonSerializer.Serialize(range);

        json.Should().Contain("\"start\":");
        json.Should().Contain("\"end\":");
    }

    [TestMethod]
    public void Deserialization_FromCamelCaseJson_Succeeds()
    {
        const string json = """{"start": 42, "end": 99}""";
        var range = JsonSerializer.Deserialize<TextRange>(json);

        range.Should().NotBeNull();
        range!.Start.Should().Be(42);
        range.End.Should().Be(99);
    }
}
