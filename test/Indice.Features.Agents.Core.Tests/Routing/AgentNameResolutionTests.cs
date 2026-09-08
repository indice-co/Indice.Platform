namespace Indice.Features.Agents.Core.Tests.Routing;

public class AgentNameResolutionTests
{
    private static readonly string[] Registered = ["auto", "knowledge", "Billing"];

    [Theory]
    [InlineData("knowledge", "knowledge")]
    [InlineData("  KNOWLEDGE ", "knowledge")]
    [InlineData("billing", "Billing")]
    public void Returns_the_registered_spelling_of_a_known_name(string requested, string expected) {
        Assert.Equal(expected, AgentsChatClient.ResolveAgentName(requested, Registered, "auto"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nope")]
    public void Falls_back_to_the_default_agent_for_missing_or_unknown_names(string? requested) {
        Assert.Equal("auto", AgentsChatClient.ResolveAgentName(requested, Registered, "auto"));
    }

    [Fact]
    public void Falls_back_to_knowledge_when_the_default_agent_is_not_registered() {
        Assert.Equal("knowledge", AgentsChatClient.ResolveAgentName("nope", ["knowledge"], "auto"));
        Assert.Equal("knowledge", AgentsChatClient.ResolveAgentName(null, ["knowledge"], null));
    }
}
