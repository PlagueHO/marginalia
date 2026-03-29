using FluentAssertions;
using Marginalia.Domain.Configuration;

namespace Marginalia.Tests.Unit.Configuration;

[TestClass]
[TestCategory("Unit")]
public sealed class LlmEndpointOptionsTests
{
    [TestMethod]
    public void SectionName_IsLlmEndpoint()
    {
        LlmEndpointOptions.SectionName.Should().Be("LlmEndpoint");
    }

    [TestMethod]
    public void Constructor_AllPropertiesNull_ByDefault()
    {
        var options = new LlmEndpointOptions();

        options.Endpoint.Should().BeNull();
        options.ModelName.Should().BeNull();
    }

    [TestMethod]
    public void Constructor_WithEndpoint_SetsValue()
    {
        var options = new LlmEndpointOptions
        {
            Endpoint = "https://my-foundry.azure.com/models"
        };

        options.Endpoint.Should().Be("https://my-foundry.azure.com/models");
    }

    [TestMethod]
    public void Constructor_WithAllFields_SetsValues()
    {
        var options = new LlmEndpointOptions
        {
            Endpoint = "https://endpoint.azure.com",
            ModelName = "gpt-5.3-chat"
        };

        options.Endpoint.Should().NotBeNullOrEmpty();
        options.ModelName.Should().Be("gpt-5.3-chat");
    }

    [TestMethod]
    public void Record_With_CreatesModifiedCopy()
    {
        var original = new LlmEndpointOptions
        {
            Endpoint = "https://old.azure.com",
            ModelName = "old-model"
        };

        var updated = original with { Endpoint = "https://new.azure.com" };

        updated.Endpoint.Should().Be("https://new.azure.com");
        updated.ModelName.Should().Be("old-model");
        original.Endpoint.Should().Be("https://old.azure.com");
    }

    [TestMethod]
    public void Endpoint_InvalidUrl_IsNotPreventedByRecord()
    {
        // Record doesn't validate URL format; validation belongs in the service layer
        var options = new LlmEndpointOptions
        {
            Endpoint = "not-a-url"
        };

        options.Endpoint.Should().Be("not-a-url");
    }
}
