using System.Text.Json;
using FluentAssertions;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Domain;

/// <summary>
/// Tests for the DocumentStatus enum used by the home page feature.
/// Verifies JSON serialization uses string representation and all expected values exist.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public sealed class DocumentStatusTests
{
    [TestMethod]
    public void Draft_SerializesToJsonString()
    {
        var json = JsonSerializer.Serialize(DocumentStatus.Draft);
        json.Should().Be("\"Draft\"");
    }

    [TestMethod]
    public void Analyzed_SerializesToJsonString()
    {
        var json = JsonSerializer.Serialize(DocumentStatus.Analyzed);
        json.Should().Be("\"Analyzed\"");
    }

    [TestMethod]
    public void Deserialization_FromDraftString_ReturnsDraft()
    {
        var status = JsonSerializer.Deserialize<DocumentStatus>("\"Draft\"");
        status.Should().Be(DocumentStatus.Draft);
    }

    [TestMethod]
    public void Deserialization_FromAnalyzedString_ReturnsAnalyzed()
    {
        var status = JsonSerializer.Deserialize<DocumentStatus>("\"Analyzed\"");
        status.Should().Be(DocumentStatus.Analyzed);
    }

    [TestMethod]
    public void AllValues_ContainExactlyDraftAndAnalyzed()
    {
        var values = Enum.GetValues<DocumentStatus>();

        values.Should().HaveCount(2);
        values.Should().Contain(DocumentStatus.Draft);
        values.Should().Contain(DocumentStatus.Analyzed);
    }

    [TestMethod]
    public void Draft_HasNumericValueZero()
    {
        ((int)DocumentStatus.Draft).Should().Be(0);
    }

    [TestMethod]
    public void Analyzed_HasNumericValueOne()
    {
        ((int)DocumentStatus.Analyzed).Should().Be(1);
    }
}
