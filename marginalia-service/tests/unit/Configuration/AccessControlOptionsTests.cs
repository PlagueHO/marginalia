using FluentAssertions;
using Marginalia.Domain.Configuration;

namespace Marginalia.Tests.Unit.Configuration;

[TestClass]
[TestCategory("Unit")]
public sealed class AccessControlOptionsTests
{
    [TestMethod]
    public void SectionName_IsAccessControl()
    {
        AccessControlOptions.SectionName.Should().Be("AccessControl");
    }

    [TestMethod]
    public void Constructor_AccessCodeNull_ByDefault()
    {
        var options = new AccessControlOptions();

        options.AccessCode.Should().BeNull();
    }

    [TestMethod]
    public void Constructor_WithAccessCode_SetsValue()
    {
        var options = new AccessControlOptions
        {
            AccessCode = "my-code-123"
        };

        options.AccessCode.Should().Be("my-code-123");
    }

    [TestMethod]
    public void Record_With_CreatesModifiedCopy()
    {
        var original = new AccessControlOptions
        {
            AccessCode = "original-code"
        };

        var updated = original with { AccessCode = "new-code" };

        updated.AccessCode.Should().Be("new-code");
        original.AccessCode.Should().Be("original-code");
    }

    [TestMethod]
    public void Constructor_WithEmptyString_SetsEmptyString()
    {
        var options = new AccessControlOptions
        {
            AccessCode = ""
        };

        options.AccessCode.Should().BeEmpty();
    }
}
