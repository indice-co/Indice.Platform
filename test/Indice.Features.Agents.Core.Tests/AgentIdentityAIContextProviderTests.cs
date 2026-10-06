using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Indice.Features.Agents.Core.Tests;

public class AgentIdentityAIContextProviderTests
{
    [Fact]
    public async Task NoIdentityFile_ContributesLibraryDefault() {
        // Content root without a Prompts folder, so the renderer falls back to the library default.
        var provider = CreateProvider(Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        var context = await InvokeAsync(provider);

        Assert.Equal(AgentsConstants.PromptDefaults.AgentIdentity, context.Instructions);
    }

    [Fact]
    public async Task IdentityFile_IsContributedVerbatim() {
        const string Identity = """
            # Dex — Agent Identity

            You are Dex, the assistant of Indice, a software & cloud hub.
            - "Identity": identity & access management <IAM>
            """;

        var context = await InvokeWithIdentityFileAsync(Identity + Environment.NewLine);

        Assert.Equal(Identity, context.Instructions);
    }

    [Fact]
    public async Task BlankIdentityFile_ContributesNoInstructions() {
        var context = await InvokeWithIdentityFileAsync(Environment.NewLine);

        Assert.True(string.IsNullOrEmpty(context.Instructions));
    }

    private static async Task<AIContext> InvokeWithIdentityFileAsync(string contents) {
        var contentRoot = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Join(contentRoot, "Prompts"));
        File.WriteAllText(Path.Join(contentRoot, "Prompts", "AgentIdentity.txt"), contents);
        try {
            return await InvokeAsync(CreateProvider(contentRoot));
        } finally {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    private static AgentIdentityAIContextProvider CreateProvider(string contentRoot) =>
        new(new FileSystemPromptTemplateRenderer(new TestHostEnvironment(contentRoot)));

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
