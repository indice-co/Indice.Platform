using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Tests;

public class AgentIdentityAIContextProviderTests
{
    [Fact]
    public async Task BlankName_ContributesNoInstructions() {
        var provider = CreateProvider(new AgentsOptions.AgentIdentityOptions {
            Company = new() { Name = "Indice" },
        });

        var context = await InvokeAsync(provider);

        Assert.True(string.IsNullOrEmpty(context.Instructions));
    }

    [Fact]
    public async Task ConfiguredIdentity_RendersDefaultTemplateUnescaped() {
        var provider = CreateProvider(new AgentsOptions.AgentIdentityOptions {
            Name = "Dex",
            Company = new() {
                Name = "Indice",
                Blurb = "a software & cloud hub — building software that connects people and business",
                Platform = "Indice.Platform",
                Products = [
                    new() { Name = "Identity", Summary = "identity & access management" },
                    new() { Name = "Messaging", Summary = "multichannel messaging" },
                ],
            },
        });

        var context = await InvokeAsync(provider);

        Assert.NotNull(context.Instructions);
        Assert.Contains("AGENT IDENTITY:", context.Instructions);
        Assert.Contains("You are Dex, the assistant of Indice.", context.Instructions);
        Assert.Contains("Indice is a software & cloud hub — building software that connects people and business.", context.Instructions);
        Assert.Contains("Its products are built on Indice.Platform.", context.Instructions);
        Assert.Contains("- Identity: identity & access management", context.Instructions);
        Assert.Contains("- Messaging: multichannel messaging", context.Instructions);
        Assert.DoesNotContain("&amp;", context.Instructions);
    }

    [Fact]
    public async Task NameOnly_OmitsCompanySections() {
        var provider = CreateProvider(new AgentsOptions.AgentIdentityOptions { Name = "Dex" });

        var context = await InvokeAsync(provider);

        Assert.NotNull(context.Instructions);
        Assert.Contains("You are Dex.", context.Instructions);
        Assert.DoesNotContain("Products:", context.Instructions);
        Assert.DoesNotContain("built on", context.Instructions);
    }

    [Fact]
    public void IdentityFile_ReplacesConfiguredIdentity() {
        var contentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(contentRoot, "Prompts"));
        // Configuration JSON tolerates trailing commas; keep one here so hand-edited files keep loading.
        File.WriteAllText(Path.Combine(contentRoot, "Prompts", AgentIdentityFileConfigureOptions.FileName), """
            {
              "Name": "Dex",
              "Company": {
                "Name": "Indice",
                "Products": [
                  { "Name": "Identity", "Summary": "identity & access management" },
                ]
              }
            }
            """);
        var options = new AgentsOptions {
            Identity = new() { Name = "Configured", Company = new() { Products = [new() { Name = "Stale" }] } },
        };
        try {
            new AgentIdentityFileConfigureOptions(new TestHostEnvironment(contentRoot)).Configure(options);
        } finally {
            Directory.Delete(contentRoot, recursive: true);
        }

        Assert.Equal("Dex", options.Identity.Name);
        Assert.Equal("Indice", options.Identity.Company.Name);
        var product = Assert.Single(options.Identity.Company.Products);
        Assert.Equal("Identity", product.Name);
    }

    [Fact]
    public void MissingIdentityFile_LeavesOptionsUntouched() {
        var options = new AgentsOptions { Identity = new() { Name = "Configured" } };

        new AgentIdentityFileConfigureOptions(new TestHostEnvironment(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")))).Configure(options);

        Assert.Equal("Configured", options.Identity.Name);
    }

    private static AgentIdentityAIContextProvider CreateProvider(AgentsOptions.AgentIdentityOptions identity) {
        var options = Options.Create(new AgentsOptions { Identity = identity });
        // Content root without a Prompts folder, so the renderer falls back to the library default template.
        var prompts = new FileSystemPromptTemplateRenderer(new TestHostEnvironment(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
        return new AgentIdentityAIContextProvider(options, prompts);
    }

    private static async Task<AIContext> InvokeAsync(AIContextProvider provider) {
        var agent = new ChatClientAgent(new NoOpChatClient());
#pragma warning disable MAAI001 // InvokingContext's constructor is experimental; it is the public way to drive a provider outside an agent run.
        return await provider.InvokingAsync(new AIContextProvider.InvokingContext(agent, null, new AIContext()));
#pragma warning restore MAAI001
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class NoOpChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}
