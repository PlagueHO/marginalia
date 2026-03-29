namespace Marginalia.Orchestration.IntegrationTests;

[TestClass]
[TestCategory("Integration")]
public class AppHostTests
{
    [TestMethod]
    public async Task ApiResource_ShouldExist()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Marginalia_AppHost>();

        await using var app = await builder.BuildAsync();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        model.Resources
            .Any(r => r.Name == "api")
            .Should()
            .BeTrue("the AppHost should define an 'api' resource");
    }

    [TestMethod]
    public async Task FrontendResource_ShouldExist()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Marginalia_AppHost>();

        await using var app = await builder.BuildAsync();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        model.Resources
            .Any(r => r.Name == "frontend")
            .Should()
            .BeTrue("the AppHost should define a 'frontend' resource");
    }
}
