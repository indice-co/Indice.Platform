using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace Indice.Features.Agents.FreeLLM.Tests;

/// <summary>Records the requests the provider sends and answers them with canned OpenAI-style responses.</summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<RecordedRequest, HttpResponseMessage> _respond;

    public StubHttpMessageHandler(Func<RecordedRequest, HttpResponseMessage>? respond = null) {
        _respond = respond ?? DefaultResponse;
    }

    public List<RecordedRequest> Requests { get; } = [];

    public RecordedRequest Single => Assert.Single(Requests);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var recorded = new RecordedRequest(request.RequestUri!, request.Headers.Authorization?.ToString(), body is null ? null : JsonNode.Parse(body)?.AsObject());
        Requests.Add(recorded);
        return _respond(recorded);
    }

    public static HttpResponseMessage DefaultResponse(RecordedRequest request) {
        var path = request.Uri.AbsolutePath;
        if (path.EndsWith("/embeddings", StringComparison.Ordinal)) {
            return Json(EmbeddingResponse(1536));
        }
        if (request.Body?["stream"]?.GetValue<bool>() == true) {
            return ServerSentEvents(
                """{"id":"c1","object":"chat.completion.chunk","created":1,"model":"served-model","choices":[{"index":0,"delta":{"role":"assistant","content":"hel"},"finish_reason":null}]}""",
                """{"id":"c1","object":"chat.completion.chunk","created":1,"model":"served-model","choices":[{"index":0,"delta":{"content":"lo"},"finish_reason":"stop"}]}""");
        }
        return Json("""{"id":"c1","object":"chat.completion","created":1,"model":"served-model","choices":[{"index":0,"message":{"role":"assistant","content":"hello"},"finish_reason":"stop"}],"usage":{"prompt_tokens":1,"completion_tokens":1,"total_tokens":2}}""");
    }

    public static string EmbeddingResponse(int dimensions) {
        var vector = string.Join(",", Enumerable.Range(0, dimensions).Select(_ => "0.5"));
        return $$$"""{"object":"list","data":[{"object":"embedding","index":0,"embedding":[{{{vector}}}]}],"model":"embedding-model","usage":{"prompt_tokens":1,"total_tokens":1}}""";
    }

    public static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage ServerSentEvents(params string[] events) {
        var payload = string.Concat(events.Select(e => $"data: {e}\n\n")) + "data: [DONE]\n\n";
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(payload, Encoding.UTF8, "text/event-stream") };
    }
}

/// <summary>A request the stub received.</summary>
internal sealed record RecordedRequest(Uri Uri, string? Authorization, JsonObject? Body);
