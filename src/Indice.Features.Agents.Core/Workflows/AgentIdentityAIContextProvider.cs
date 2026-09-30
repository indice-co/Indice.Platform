using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows;

/// <summary>
/// Contributes the assistant's identity (who it is, who it represents, what the company's products are) as additional
/// instructions on every agent invocation. Renders the <c>AgentIdentity</c> prompt template once from
/// <see cref="AgentsOptions.Identity"/> and reuses the result, so a single provider instance is safe to attach to any agent.
/// Returns an empty <see cref="AIContext"/> when <see cref="AgentsOptions.AgentIdentityOptions.Name"/> is blank.
/// </summary>
public sealed class AgentIdentityAIContextProvider : AIContextProvider {
    private readonly Lazy<string?> _instructions;

    /// <summary>Creates a new <see cref="AgentIdentityAIContextProvider"/>.</summary>
    public AgentIdentityAIContextProvider(IOptions<AgentsOptions> options, IPromptTemplateRenderer prompts) : base(null, null) {
        _instructions = new Lazy<string?>(() => Render(options.Value.Identity, prompts));
    }

    /// <inheritdoc/>
    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default) {
        var instructions = _instructions.Value;
        return ValueTask.FromResult(instructions is null ? new AIContext() : new AIContext { Instructions = instructions });
    }

    private static string? Render(AgentsOptions.AgentIdentityOptions identity, IPromptTemplateRenderer prompts) {
        if (string.IsNullOrWhiteSpace(identity.Name)) {
            return null;
        }
        var company = identity.Company;
        var rendered = prompts.Render("AgentIdentity", new {
            agent = new { name = identity.Name },
            company = new {
                name = company.Name,
                blurb = company.Blurb,
                platform = company.Platform,
                products = company.Products.Select(product => new { name = product.Name, summary = product.Summary }).ToList(),
            },
        });
        return string.IsNullOrWhiteSpace(rendered) ? null : rendered.Trim();
    }
}
