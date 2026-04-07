using System.Text.Json;
using FluentAssertions;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Domain;

[TestClass]
[TestCategory("Unit")]
public sealed class AccessControlStatusResponseTests
{
    [TestMethod]
    public void AccessCodeRequired_True_SerializesCorrectly()
    {
        var response = new AccessControlStatusResponse { AccessCodeRequired = true };

        var json = JsonSerializer.Serialize(response);

        json.Should().Contain("\"accessCodeRequired\":true");
    }

    [TestMethod]
    public void AccessCodeRequired_False_SerializesCorrectly()
    {
        var response = new AccessControlStatusResponse { AccessCodeRequired = false };

        var json = JsonSerializer.Serialize(response);

        json.Should().Contain("\"accessCodeRequired\":false");
    }

    [TestMethod]
    public void Deserialization_FromJson_Works()
    {
        var json = """{"accessCodeRequired":true}""";

        var response = JsonSerializer.Deserialize<AccessControlStatusResponse>(json);

        response.Should().NotBeNull();
        response!.AccessCodeRequired.Should().BeTrue();
    }

    [TestMethod]
    public void Record_With_CreatesModifiedCopy()
    {
        var original = new AccessControlStatusResponse { AccessCodeRequired = true };

        var updated = original with { AccessCodeRequired = false };

        updated.AccessCodeRequired.Should().BeFalse();
        original.AccessCodeRequired.Should().BeTrue();
    }
}
