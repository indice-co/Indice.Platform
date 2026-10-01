using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Agents.AI;

namespace Indice.Features.Agents.Core.Workflows;

/// <summary>
/// Contributes the assistant's identity (who it is, who it represents, what it knows about) as additional instructions on
/// every agent invocation. The text is the <c>AgentIdentity</c> prompt template, i.e. the host's free-text
/// <c>Prompts/AgentIdentity.txt</c> or the short generic library default, rendered once and reused, so a single provider
/// instance is safe to attach to any agent. Returns an empty <see cref="AIContext"/> when the host's file is blank.
/// </summary>
public sealed class AgentIdentityAIContextProvider : AIContextProvider {
    private readonly Lazy<string?> _instructions;

    /// <summary>Creates a new <see cref="AgentIdentityAIContextProvider"/>.</summary>
    public AgentIdentityAIContextProvider(IPromptTemplateRenderer prompts) : base(null, null) {
        _instructions = new Lazy<string?>(() => {
            var rendered = prompts.Render(nameof(AgentsConstants.PromptDefaults.AgentIdentity));
            return string.IsNullOrWhiteSpace(rendered) ? null : rendered.Trim();
        });
    }

    /// <inheritdoc/>
    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default) {
        var instructions = _instructions.Value;
        return ValueTask.FromResult(instructions is null ? new AIContext() : new AIContext { Instructions = instructions });
    }
}
