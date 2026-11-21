using Indice.Features.Agents.Core;
using Indice.Features.Agents.Core.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.FreeLLM.Tests;

public class FreeLLMModelProviderTests
{
    private const string Endpoint = "http://gateway.test/v1";

    /// <param name="withDefaults">Pre-fills the endpoint and all three models, which <paramref name="configure"/> and the configuration can still override.</param>
    private static ServiceProvider BuildServices(StubHttpMessageHandler handler, Dictionary<string, string?>? settings = null, Action<FreeLLMOptions>? configure = null, bool withDefaults = true) {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings ?? []).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        new AgentsModelProviderBuilder(services).UseFreeLLM(options => {
            if (withDefaults) {
                options.Endpoint ??= Endpoint;
                options.Models.Reasoning ??= "reasoning-model";
                options.Models.Fast ??= "fast-model";
                options.Models.Embedding ??= "embedding-model";
            }
            configure?.Invoke(options);
        });
        services.AddHttpClient(FreeLLMOptions.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider();
    }

    private static IAgentsModelProvider ProviderOf(ServiceProvider services) => Assert.Single(services.GetServices<IAgentsModelProvider>());

    private static IChatClient ChatClientOf(ServiceProvider services, AgentsModelRole role = AgentsModelRole.Reasoning) => ProviderOf(services).CreateChatClient(role);

    private static readonly ChatMessage[] Hello = [new(ChatRole.User, "hi")];

    [Theory]
    [InlineData("http://gateway.test/v1")]
    [InlineData("http://gateway.test/v1/")]
    public async Task Chat_requests_go_to_the_configured_endpoint_with_the_role_model_and_key(string endpoint) {
        var handler = new StubHttpMessageHandler();
        using var services = BuildServices(handler, configure: o => { o.Endpoint = endpoint; o.ApiKey = "secret"; });

        var reasoning = await ChatClientOf(services, AgentsModelRole.Reasoning).GetResponseAsync(Hello, cancellationToken: TestContext.Current.CancellationToken);
        var fast = await ChatClientOf(services, AgentsModelRole.Fast).GetResponseAsync(Hello, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("hello", reasoning.Text);
        Assert.Equal("hello", fast.Text);
        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request => {
            Assert.Equal("http://gateway.test/v1/chat/completions", request.Uri.ToString());
            Assert.Equal("Bearer secret", request.Authorization);
        });
        Assert.Equal("reasoning-model", handler.Requests[0].Body!["model"]!.GetValue<string>());
        Assert.Equal("fast-model", handler.Requests[1].Body!["model"]!.GetValue<string>());
    }

    [Fact]
    public async Task A_placeholder_key_is_sent_when_the_endpoint_needs_none() {
        var handler = new StubHttpMessageHandler();
        using var services = BuildServices(handler);

        await ChatClientOf(services).GetResponseAsync(Hello, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Bearer unused", handler.Single.Authorization);
    }

    [Fact]
    public async Task The_output_limit_is_sent_as_max_tokens_by_default() {
        var handler = new StubHttpMessageHandler();
        using var services = BuildServices(handler);

        await ChatClientOf(services).GetResponseAsync(Hello, new ChatOptions { MaxOutputTokens = 256 }, TestContext.Current.CancellationToken);

        var body = handler.Single.Body!;
        Assert.Equal(256, body["max_tokens"]!.GetValue<int>());
        Assert.False(body.ContainsKey("max_completion_tokens"));
    }

    [Fact]
    public async Task The_output_limit_keeps_max_completion_tokens_when_max_tokens_is_switched_off() {
        var handler = new StubHttpMessageHandler();
        using var services = BuildServices(handler, configure: o => o.Compatibility.UseMaxTokens = false);

        await ChatClientOf(services).GetResponseAsync(Hello, new ChatOptions { MaxOutputTokens = 256 }, TestContext.Current.CancellationToken);

        var body = handler.Single.Body!;
        Assert.Equal(256, body["max_completion_tokens"]!.GetValue<int>());
        Assert.False(body.ContainsKey("max_tokens"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Streaming_requests_usage_unless_it_is_switched_off(bool streamUsage) {
        var handler = new StubHttpMessageHandler();
        using var services = BuildServices(handler, configure: o => o.Compatibility.StreamUsage = streamUsage);

        var text = string.Concat(await ChatClientOf(services).GetStreamingResponseAsync(Hello, cancellationToken: TestContext.Current.CancellationToken)
            .Select(update => update.Text).ToListAsync(TestContext.Current.CancellationToken));

        Assert.Equal("hello", text);
        Assert.Equal(streamUsage, handler.Single.Body!.ContainsKey("stream_options"));
    }

    [Fact]
    public async Task Typed_replies_use_a_json_schema_by_default() {
        var handler = new StubHttpMessageHandler();
        using var services = BuildServices(handler);
        var options = new ChatOptions { ResponseFormat = ChatResponseFormat.ForJsonSchema<Person>() };

        await ChatClientOf(services).GetResponseAsync(Hello, options, TestContext.Current.CancellationToken);

        Assert.Equal("json_schema", handler.Single.Body!["response_format"]!["type"]!.GetValue<string>());
    }

    [Fact]
    public async Task Typed_replies_fall_back_to_a_json_object_with_the_schema_in_the_instructions() {
        var handler = new StubHttpMessageHandler();
        using var services = BuildServices(handler, configure: o => o.Compatibility.StructuredOutput = FreeLLMStructuredOutput.JsonObject);
        var options = new ChatOptions { Instructions = "Be brief.", ResponseFormat = ChatResponseFormat.ForJsonSchema<Person>() };

        await ChatClientOf(services).GetResponseAsync(Hello, options, TestContext.Current.CancellationToken);

        var body = handler.Single.Body!;
        Assert.Equal("json_object", body["response_format"]!["type"]!.GetValue<string>());
        var system = body["messages"]!.AsArray().First()!["content"]!.ToJsonString();
        Assert.Contains("Be brief.", system);
        Assert.Contains("JSON schema", system);
        Assert.Contains("name", system);
        // The caller's options are shared across requests and must stay untouched.
        Assert.Equal("Be brief.", options.Instructions);
        Assert.IsType<ChatResponseFormatJson>(options.ResponseFormat);
        Assert.NotNull(((ChatResponseFormatJson)options.ResponseFormat).Schema);
    }

    [Fact]
    public async Task Embeddings_share_the_endpoint_and_key_by_default() {
        var handler = new StubHttpMessageHandler();
        using var services = BuildServices(handler, configure: o => o.ApiKey = "secret");

        var embeddings = await ProviderOf(services).CreateEmbeddingGenerator().GenerateAsync(["text"], cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(AgentsOptions.EmbeddingDimensionsDefault, Assert.Single(embeddings).Vector.Length);
        var request = handler.Single;
        Assert.Equal("http://gateway.test/v1/embeddings", request.Uri.ToString());
        Assert.Equal("Bearer secret", request.Authorization);
        Assert.Equal("embedding-model", request.Body!["model"]!.GetValue<string>());
        Assert.False(request.Body.ContainsKey("dimensions"));
    }

    [Fact]
    public async Task A_separate_embeddings_host_never_receives_the_chat_key() {
        var handler = new StubHttpMessageHandler();
        using var services = BuildServices(handler, configure: o => { o.ApiKey = "chat-secret"; o.EmbeddingEndpoint = "http://embeddings.test/v1"; });

        await ProviderOf(services).CreateEmbeddingGenerator().GenerateAsync(["text"], cancellationToken: TestContext.Current.CancellationToken);

        var request = handler.Single;
        Assert.Equal("http://embeddings.test/v1/embeddings", request.Uri.ToString());
        Assert.Equal("Bearer unused", request.Authorization);
    }

    [Fact]
    public async Task A_separate_embeddings_host_gets_its_own_key() {
        var handler = new StubHttpMessageHandler();
        using var services = BuildServices(handler, configure: o => { o.ApiKey = "chat-secret"; o.EmbeddingEndpoint = "http://embeddings.test/v1"; o.EmbeddingApiKey = "embedding-secret"; });

        await ProviderOf(services).CreateEmbeddingGenerator().GenerateAsync(["text"], cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Bearer embedding-secret", handler.Single.Authorization);
    }

    [Fact]
    public async Task The_embedding_width_is_requested_when_configured() {
        var handler = new StubHttpMessageHandler();
        using var services = BuildServices(handler, configure: o => o.EmbeddingDimensions = AgentsOptions.EmbeddingDimensionsDefault);

        await ProviderOf(services).CreateEmbeddingGenerator().GenerateAsync(["text"], cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(AgentsOptions.EmbeddingDimensionsDefault, handler.Single.Body!["dimensions"]!.GetValue<int>());
    }

    [Fact]
    public async Task Vectors_of_the_wrong_width_are_rejected_with_a_clear_message() {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(StubHttpMessageHandler.EmbeddingResponse(768)));
        using var services = BuildServices(handler);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await ProviderOf(services).CreateEmbeddingGenerator().GenerateAsync(["text"], cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("768", exception.Message);
        Assert.Contains("1536", exception.Message);
    }

    [Fact]
    public void The_provider_serves_only_the_roles_that_have_a_model() {
        using var services = BuildServices(new StubHttpMessageHandler(), withDefaults: false, configure: o => { o.Endpoint = Endpoint; o.Models.Reasoning = "reasoning-model"; });

        var provider = ProviderOf(services);

        Assert.True(provider.Serves(AgentsModelRole.Reasoning));
        Assert.False(provider.Serves(AgentsModelRole.Fast));
        Assert.False(provider.Serves(AgentsModelRole.Embedding));
    }

    [Fact]
    public void Without_any_model_the_provider_is_inactive_and_needs_no_endpoint() {
        using var services = BuildServices(new StubHttpMessageHandler(), withDefaults: false);

        var provider = ProviderOf(services);

        Assert.All(Enum.GetValues<AgentsModelRole>(), role => Assert.False(provider.Serves(role)));
        Assert.NotNull(services.GetRequiredService<IOptions<FreeLLMOptions>>().Value);
    }

    [Fact]
    public void An_embedding_only_setup_can_use_just_the_embedding_endpoint() {
        using var services = BuildServices(new StubHttpMessageHandler(), withDefaults: false, configure: o => {
            o.Models.Embedding = "embedding-model";
            o.EmbeddingEndpoint = "http://embeddings.test/v1";
        });

        Assert.NotNull(services.GetRequiredService<IOptions<FreeLLMOptions>>().Value);
        Assert.True(ProviderOf(services).Serves(AgentsModelRole.Embedding));
    }

    [Theory]
    [InlineData("chat model without endpoint", null, null, null, null)]
    [InlineData("chat model with a relative endpoint", "/v1", null, null, null)]
    [InlineData("chat model with a non-http endpoint", "ftp://gateway.test/v1", null, null, null)]
    [InlineData("wrong embedding width", Endpoint, 768, null, null)]
    [InlineData("zero timeout", Endpoint, null, "00:00:00", null)]
    [InlineData("embedding model with an invalid embedding endpoint", Endpoint, null, null, "not a url")]
    public void Invalid_settings_are_rejected(string scenario, string? endpoint, int? dimensions, string? timeout, string? embeddingEndpoint) {
        _ = scenario;
        using var services = BuildServices(new StubHttpMessageHandler(), withDefaults: false, configure: o => {
            o.Endpoint = endpoint;
            o.Models.Reasoning = "reasoning-model";
            o.Models.Embedding = embeddingEndpoint is null ? null : "embedding-model";
            o.EmbeddingDimensions = dimensions;
            o.EmbeddingEndpoint = embeddingEndpoint;
            o.NetworkTimeout = timeout is null ? null : TimeSpan.Parse(timeout);
        });

        Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IOptions<FreeLLMOptions>>().Value);
    }

    [Fact]
    public void Settings_bind_from_configuration_and_code_overrides_them() {
        var settings = new Dictionary<string, string?> {
            ["Dex:FreeLLM:Endpoint"] = "http://from-config.test/v1",
            ["Dex:FreeLLM:ApiKey"] = "from-config",
            ["Dex:FreeLLM:Models:Reasoning"] = "config-reasoning",
            ["Dex:FreeLLM:NetworkTimeout"] = "00:05:00",
            ["Dex:FreeLLM:Compatibility:StructuredOutput"] = "JsonObject",
            ["Dex:FreeLLM:Compatibility:StreamUsage"] = "false",
        };
        using var services = BuildServices(new StubHttpMessageHandler(), settings, o => o.ApiKey = "from-code");

        var options = services.GetRequiredService<IOptions<FreeLLMOptions>>().Value;

        Assert.Equal("http://from-config.test/v1", options.Endpoint);
        Assert.Equal("from-code", options.ApiKey);
        Assert.Equal("config-reasoning", options.Models.Reasoning);
        Assert.Equal(TimeSpan.FromMinutes(5), options.NetworkTimeout);
        Assert.Equal(FreeLLMStructuredOutput.JsonObject, options.Compatibility.StructuredOutput);
        Assert.False(options.Compatibility.StreamUsage);
        Assert.True(options.Compatibility.UseMaxTokens);
    }

    [Fact]
    public async Task FreeLLM_and_Azure_AI_Foundry_can_serve_different_roles() {
        var settings = new Dictionary<string, string?> {
            ["Dex:FreeLLM:Endpoint"] = Endpoint,
            ["Dex:FreeLLM:Models:Reasoning"] = "local-reasoning",
            ["Dex:AzureOpenAI:Endpoint"] = "https://example.openai.azure.com",
            ["Dex:AzureOpenAI:ApiKey"] = "secret",
            ["Dex:AzureOpenAI:Deployments:Fast"] = "azure-fast",
            ["Dex:AzureOpenAI:Deployments:Embedding"] = "azure-embedding",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var handler = new StubHttpMessageHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddAgentsCore(configuration, configureModelProviders: providers => providers.UseFreeLLM());
        services.AddHttpClient(FreeLLMOptions.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();

        // The Azure Reasoning deployment is not required, because FreeLLM serves that role.
        Assert.NotNull(provider.GetRequiredService<IOptions<AgentsOptions>>().Value);
        var reasoning = provider.GetRequiredKeyedService<IChatClient>(nameof(AgentsModelRole.Reasoning));
        var fast = provider.GetRequiredKeyedService<IChatClient>(nameof(AgentsModelRole.Fast));
        Assert.Equal("local-reasoning", reasoning.GetService<ChatClientMetadata>()?.DefaultModelId);
        Assert.Equal("azure-fast", fast.GetService<ChatClientMetadata>()?.DefaultModelId);
        Assert.Equal("azure-embedding", provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>().GetService<EmbeddingGeneratorMetadata>()?.DefaultModelId);

        await reasoning.GetResponseAsync(Hello, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("http://gateway.test/v1/chat/completions", handler.Single.Uri.ToString());
    }

    [Fact]
    public async Task FreeLLM_alone_is_enough_for_every_role() {
        var settings = new Dictionary<string, string?> {
            ["Dex:FreeLLM:Endpoint"] = Endpoint,
            ["Dex:FreeLLM:Models:Reasoning"] = "reasoning-model",
            ["Dex:FreeLLM:Models:Fast"] = "fast-model",
            ["Dex:FreeLLM:Models:Embedding"] = "embedding-model",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddAgentsCore(configuration, configureModelProviders: providers => providers.UseFreeLLM());
        services.AddHttpClient(FreeLLMOptions.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler());
        await using var provider = services.BuildServiceProvider();

        // No Dex:AzureOpenAI settings at all.
        Assert.NotNull(provider.GetRequiredService<IOptions<AgentsOptions>>().Value);
        Assert.Equal("fast-model", provider.GetRequiredKeyedService<IChatClient>(nameof(AgentsModelRole.Fast)).GetService<ChatClientMetadata>()?.DefaultModelId);
        Assert.Equal("embedding-model", provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>().GetService<EmbeddingGeneratorMetadata>()?.DefaultModelId);
        // The decision model is off by default and needs no Azure settings, so the "auto" router can be built.
        Assert.NotNull(provider.GetRequiredService<Core.Services.ISystemOneClient>());
    }

    private sealed record Person(string Name, int Age);
}
