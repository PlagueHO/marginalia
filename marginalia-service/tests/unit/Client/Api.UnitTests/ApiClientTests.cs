namespace Marginalia.Api.Client.UnitTests;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ClientApiClient = global::Marginalia.Api.Client.ApiClient;
using IApiClient = global::Marginalia.Api.Client.IApiClient;

[TestClass]
[TestCategory("Unit")]
public sealed class ApiClientTests
{
    [TestMethod]
    public async Task GetAsync_ReturnsDeserializedObject()
    {
        var expected = new TestDto { Value = "foo" };
        var client = CreateClient(HttpStatusCode.OK, expected);

        var result = await client.GetAsync<TestDto>("test");

        result.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task PostAsync_ReturnsDeserializedObject()
    {
        var request = new TestDto { Value = "bar" };
        var expected = new TestDto { Value = "baz" };
        var client = CreateClient(HttpStatusCode.OK, expected);

        var result = await client.PostAsync<TestDto, TestDto>("test", request);

        result.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task PutAsync_ReturnsDeserializedObject()
    {
        var request = new TestDto { Value = "bar" };
        var expected = new TestDto { Value = "baz" };
        var client = CreateClient(HttpStatusCode.OK, expected);

        var result = await client.PutAsync<TestDto, TestDto>("test", request);

        result.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task DeleteAsync_SucceedsOnSuccessStatusCode()
    {
        var client = CreateClient<object?>(HttpStatusCode.NoContent, null);

        await client.DeleteAsync("test");
    }

    [TestMethod]
    public async Task GetAsync_ThrowsOnErrorStatusCode()
    {
        var client = CreateClient<TestDto?>(HttpStatusCode.BadRequest, null);

        var act = () => client.GetAsync<TestDto>("test");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private static IApiClient CreateClient<T>(HttpStatusCode statusCode, T responseBody)
    {
        var handler = new TestMessageHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/api/")
        };

        return new ClientApiClient(httpClient);
    }

    private sealed class TestDto
    {
        public string? Value { get; set; }
    }

    private sealed class TestMessageHandler(HttpStatusCode statusCode, object? responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode);

            if (responseBody is not null)
            {
                response.Content = JsonContent.Create(responseBody);
            }

            return Task.FromResult(response);
        }
    }
}
