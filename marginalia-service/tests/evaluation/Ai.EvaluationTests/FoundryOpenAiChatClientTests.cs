using System.Net;
using System.Text.Json;
using Azure.Core;
using FluentAssertions;
using Microsoft.Extensions.AI;

namespace Marginalia.Ai.EvaluationTests;

[TestClass]
public sealed class FoundryOpenAiChatClientTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetResponseAsyncIncludesJsonSchemaResponseFormatWhenRequested()
    {
        var handler = new CapturingHandler();
        var client = CreateClient(handler);
        var schema = JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "score": {
                  "type": "integer"
                }
              },
              "required": ["score"]
            }
            """).RootElement.Clone();

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "grade this response")],
            new ChatOptions
            {
                Temperature = 0.25f,
                ResponseFormat = ChatResponseFormat.ForJsonSchema(schema, "judge-output", "Structured judge output")
            });

        response.Text.Should().Be("ok");
        handler.RequestBody.Should().NotBeNull();

        using var payload = JsonDocument.Parse(handler.RequestBody!);
        payload.RootElement.GetProperty("model").GetString().Should().Be("judge");
        payload.RootElement.GetProperty("temperature").GetSingle().Should().Be(0.25f);
        payload.RootElement.GetProperty("response_format").GetProperty("type").GetString().Should().Be("json_schema");
        payload.RootElement.GetProperty("response_format").GetProperty("json_schema").GetProperty("name").GetString().Should().Be("judge-output");
        payload.RootElement.GetProperty("response_format").GetProperty("json_schema").GetProperty("strict").GetBoolean().Should().BeTrue();
        payload.RootElement.GetProperty("response_format").GetProperty("json_schema").GetProperty("schema").GetProperty("required")[0].GetString().Should().Be("score");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetResponseAsyncUsesJsonObjectFormatWhenSchemaMissing()
    {
        var handler = new CapturingHandler();
        var client = CreateClient(handler);

        await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "return json")],
            new ChatOptions
            {
                ResponseFormat = new ChatResponseFormatJson(schema: null, schemaName: null, schemaDescription: null)
            });

        handler.RequestBody.Should().NotBeNull();

        using var payload = JsonDocument.Parse(handler.RequestBody!);
        payload.RootElement.GetProperty("response_format").GetProperty("type").GetString().Should().Be("json_object");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetResponseAsyncAcceptsOutputTextContentParts()
    {
        var handler = new CapturingHandler("""{"choices":[{"message":{"content":[{"type":"output_text","text":"ok"}]}}]}""");
        var client = CreateClient(handler);

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "grade this response")]);

        response.Text.Should().Be("ok");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetResponseAsyncAcceptsNestedTextValueContentParts()
    {
        var handler = new CapturingHandler("""{"choices":[{"message":{"content":[{"type":"output_text","text":{"value":"ok"}}]}}]}""");
        var client = CreateClient(handler);

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "grade this response")]);

        response.Text.Should().Be("ok");
    }

    private static FoundryOpenAiChatClient CreateClient(CapturingHandler handler) =>
        new(
            new HttpClient(handler),
            new StaticTokenCredential(),
            new Uri("https://example.services.ai.azure.com/api/projects/marginalia"),
            "judge");

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }

        private readonly string _responseBody;

        public CapturingHandler(string responseBody = """{"choices":[{"message":{"content":"ok"}}]}""")
        {
            _responseBody = responseBody;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody)
            };
        }
    }

    private sealed class StaticTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new("token", DateTimeOffset.UtcNow.AddMinutes(5));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GetToken(requestContext, cancellationToken));
    }
}
