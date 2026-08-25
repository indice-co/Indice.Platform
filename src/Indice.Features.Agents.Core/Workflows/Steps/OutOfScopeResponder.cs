using System.Text.Json;
using Indice.Features.Agents.Core.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Terminal branch of the pipeline when <c>IntentClassifier</c> decides the question is out-of-scope.
/// Projects the classifier's <see cref="Intent.OutOfScopeReason"/> into a final
/// <see cref="GroundedAnswerOutput"/> envelope with no citations, and offers the configured subject
/// areas as a <see cref="AgentsConstants.MediaTypes.MultipleChoice"/> part so the user can get back
/// on a supported topic in one click.
/// </summary>
public sealed class OutOfScopeResponder : Executor<IntentOutput, GroundedAnswerOutput>
{
    private readonly AgentsOptions.TaxonomyOptions _taxonomy;

    /// <summary>Creates a new <see cref="OutOfScopeResponder"/>.</summary>
    /// <param name="options">The bound agents options; its taxonomy supplies the offered subject areas.</param>
    public OutOfScopeResponder(IOptions<AgentsOptions> options) : base("OutOfScopeResponder") {
        _taxonomy = options.Value.Taxonomy;
    }

    /// <inheritdoc/>
    public override async ValueTask<GroundedAnswerOutput> HandleAsync(
        IntentOutput intentResult,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        var reason = intentResult.Intent.OutOfScopeReason ?? "Sorry, that question is outside the scope of what I can answer here.";
        var contents = new List<AIContent> { new TextContent(reason) };
        // Distinct because Dex:Taxonomy configuration currently appends to the TaxonomyOptions defaults rather than
        // replacing them — a separate config-binding defect that would otherwise surface as duplicate options.
        var categories = _taxonomy.Categories.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (categories.Count > 0) {
            // The option string is both the label and the message posted when picked, so it has to read as a question.
            var choice = new MultipleChoice {
                Options = [.. categories.Select(category => $"What can you tell me about {category}?")]
            };
            contents.Add(new DataContent(JsonSerializer.SerializeToUtf8Bytes(choice), AgentsConstants.MediaTypes.MultipleChoice));
        }
        await context.AddEventAsync(new AgentResponseUpdateEvent(Id,
                                        new AgentResponseUpdate(ChatRole.Assistant, contents)
                                    ), cancellationToken);
        return new GroundedAnswerOutput(reason, [], []);
    }
}
