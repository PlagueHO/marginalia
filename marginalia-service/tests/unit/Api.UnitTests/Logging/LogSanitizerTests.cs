using FluentAssertions;
using Marginalia.ServiceDefaults.Logging;

namespace Marginalia.Api.UnitTests.Logging;

[TestClass]
[TestCategory("Unit")]
public sealed class LogSanitizerTests
{
    [TestMethod]
    public void Sanitize_ReturnsNull_WhenInputIsNull()
    {
        var result = LogSanitizer.Sanitize(null);

        result.Should().BeNull();
    }

    [TestMethod]
    public void Sanitize_EscapesControlCharacters()
    {
        var result = LogSanitizer.Sanitize("alpha\rbeta\ngamma\tdelta\u0001");

        result.Should().Be("alpha\\rbeta\\ngamma\\tdelta\\u0001");
    }

    [TestMethod]
    public void Sanitize_ReturnsOriginalString_WhenNoEscapingRequired()
    {
        var input = "plain printable value";

        var result = LogSanitizer.Sanitize(input);

        result.Should().BeSameAs(input);
    }

    [TestMethod]
    public void SanitizeAttributes_SanitizesOnlyStringValues()
    {
        IReadOnlyList<KeyValuePair<string, object?>> attributes =
        [
            new KeyValuePair<string, object?>("safe", "value"),
            new KeyValuePair<string, object?>("unsafe", "line1\nline2"),
            new KeyValuePair<string, object?>("count", 12)
        ];

        var result = LogSanitizer.SanitizeAttributes(attributes);

        result.Should().NotBeSameAs(attributes);
        result.Should().HaveCount(3);
        result![0].Key.Should().Be("safe");
        result[0].Value.Should().Be("value");
        result[1].Key.Should().Be("unsafe");
        result[1].Value.Should().Be("line1\\nline2");
        result[2].Key.Should().Be("count");
        result[2].Value.Should().Be(12);
    }

    [TestMethod]
    public void SanitizeAttributes_ReturnsOriginal_WhenNoStringValuesRequireChanges()
    {
        IReadOnlyList<KeyValuePair<string, object?>> attributes =
        [
            new KeyValuePair<string, object?>("status", "ok"),
            new KeyValuePair<string, object?>("count", 2)
        ];

        var result = LogSanitizer.SanitizeAttributes(attributes);

        result.Should().BeSameAs(attributes);
    }
}
