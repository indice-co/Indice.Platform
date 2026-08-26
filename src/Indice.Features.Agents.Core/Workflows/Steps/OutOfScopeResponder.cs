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
/// <see cref="GroundedAnswerOutput"/> envelope with no citations, and dresses the refusal with every alternative
/// content part the chat UI can render, so a user who has hit the edge of the knowledge base is handed a way back in.
/// </summary>
/// <remarks>
/// This step is the reference producer for the rendering contracts in <see cref="AgentsConstants.MediaTypes"/>: it
/// emits a callout, an image, a confirmation and a multiple-choice list in one turn, which is what makes those media
/// types exercisable end to end (stream, persist, reload) rather than only in the SPA's part gallery.
/// </remarks>
public sealed class OutOfScopeResponder : Executor<IntentOutput, GroundedAnswerOutput>
{
    /// <summary>Fallback when the classifier gives no reason of its own.</summary>
    private const string DefaultReason = "Sorry, that question is outside the scope of what I can answer here.";

    /// <summary>
    /// The chat SPA serves its own logo from the site root, so a root-relative URL is enough — this step has no
    /// business knowing the host's public base address, and the asset is already cached by the thread's avatar.
    /// </summary>
    private const string LogoUrl = "/dex-logo.png";

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
        var reason = intentResult.Intent.OutOfScopeReason ?? DefaultReason;
        await context.AddEventAsync(new AgentResponseUpdateEvent(Id,
                                        new AgentResponseUpdate(ChatRole.Assistant, BuildContents(reason, _taxonomy.Categories))
                                    ), cancellationToken);
        return new GroundedAnswerOutput(reason, [], []);
    }

    /// <summary>
    /// Builds the contents of the refusal turn. Split out from <see cref="HandleAsync"/> so the emitted shape can be
    /// asserted without standing up a workflow context.
    /// </summary>
    /// <param name="reason">Why the question could not be answered; becomes the turn's prose.</param>
    /// <param name="categories">The configured subject areas offered as one-click follow-up questions.</param>
    public static List<AIContent> BuildContents(string reason, IEnumerable<string> categories) {
        var contents = new List<AIContent> {
            // Kept as prose rather than folded into the callout so DexChatResponse.Text stays non-empty for consumers
            // that read the answer as text rather than rendering parts.
            new TextContent(reason),
            Part(new Callout {
                Severity = Callout.Severities.Warning,
                Title = "Outside my knowledge base",
                Text = "I answer only from the internal documentation loaded into this assistant, so anything beyond it I have to turn down."
            }, AgentsConstants.MediaTypes.Callout),
            Part(new ImageReference {
                Url = LogoUrl,
                Alt = "Dex",
                Caption = "Dex answers from your knowledge base."
            }, AgentsConstants.MediaTypes.Image),
            // The affirmative label is itself an in-scope question — it classifies as the purpose category and gets a
            // real answer from PurposeResponder, so the button leads somewhere instead of looping back to this step.
            Part(new Confirmation {
                Prompt = "Want a hand finding something I do cover?",
                ConfirmText = "Yes, what can you help with?",
                CancelText = "No, thanks"
            }, AgentsConstants.MediaTypes.Confirmation)
        };
        // Distinct because Dex:Taxonomy configuration currently appends to the TaxonomyOptions defaults rather than
        // replacing them — a separate config-binding defect that would otherwise surface as duplicate options.
        var subjects = categories.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (subjects.Count > 0) {
            // The option string is both the label and the message posted when picked, so it has to read as a question.
            contents.Add(Part(new MultipleChoice {
                Options = [.. subjects.Select(category => $"What can you tell me about {category}?")]
            }, AgentsConstants.MediaTypes.MultipleChoice));
        }
        return contents;
    }

    /// <summary>Serializes a payload into the atomic <see cref="DataContent"/> part its media type stands for.</summary>
    private static DataContent Part<TPayload>(TPayload payload, string mediaType)
        => new(JsonSerializer.SerializeToUtf8Bytes(payload), mediaType);
}
