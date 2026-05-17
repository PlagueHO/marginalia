namespace Marginalia.Api.Client.UnitTests;

using FluentAssertions;
using ClientApiClient = global::Marginalia.Api.Client.ApiClient;
using IApiClient = global::Marginalia.Api.Client.IApiClient;
using global::Marginalia.Api.Client.Extensions;
using Microsoft.Extensions.DependencyInjection;

[TestClass]
[TestCategory("Unit")]
public sealed class ServiceCollectionExtensionsTests
{
    [TestMethod]
    public void AddMarginaliaApiClient_RegistersIApiClient()
    {
        var services = new ServiceCollection();

        services.AddMarginaliaApiClient(options =>
        {
            options.BaseUrl = "https://localhost:5001";
        });

        using var provider = services.BuildServiceProvider();

        var client = provider.GetService<IApiClient>();

        client.Should().NotBeNull();
        client.Should().BeOfType<ClientApiClient>();
    }

    [TestMethod]
    public void AddMarginaliaApiClient_ConfiguresBaseUrl()
    {
        var services = new ServiceCollection();

        services.AddMarginaliaApiClient(options =>
        {
            options.BaseUrl = "https://localhost:5001";
        });

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IApiClient>();
        var typedClient = client.Should().BeOfType<ClientApiClient>().Subject;
        var field = typeof(ClientApiClient).GetField("_httpClient", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var httpClient = field!.GetValue(typedClient).Should().BeOfType<HttpClient>().Subject;

        httpClient.BaseAddress.Should().Be(new Uri("https://localhost:5001/"));
    }

    [TestMethod]
    public void AddMarginaliaApiClient_ThrowsForMissingBaseUrl()
    {
        var services = new ServiceCollection();

        var act = () => services.AddMarginaliaApiClient(options =>
        {
            options.BaseUrl = string.Empty;
        });

        act.Should().Throw<ArgumentException>()
            .WithMessage("BaseUrl must be configured.*");
    }

    [TestMethod]
    public void AddMarginaliaApiClient_ThrowsForRelativeBaseUrl()
    {
        var services = new ServiceCollection();

        var act = () => services.AddMarginaliaApiClient(options =>
        {
            options.BaseUrl = "/api";
        });

        act.Should().Throw<ArgumentException>()
            .WithMessage("BaseUrl must be a valid absolute HTTP or HTTPS URI.*");
    }
}
