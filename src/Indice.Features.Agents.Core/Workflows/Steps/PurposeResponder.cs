using System.Net.Mime;
using System.Text;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Events;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Terminal branch of the pipeline when <c>IntentClassifier</c> decides the
/// question is a general question about the capabilities of the agent.
/// </summary>
internal class PurposeResponder : Executor<IntentOutput, GroundedAnswerOutput>
{

    /// <summary> The sample image embedded in this assembly, or empty if the assembly was built without it. </summary>
    private static readonly ReadOnlyMemory<byte> _logo = ReadSampleLogo();

    private readonly AIAgent _agent;
    private readonly AgentsOptions _options;
    private readonly string _model;


    /// <summary>Creates a new <see cref="PurposeResponder"/>.</summary>
    public PurposeResponder(
        [FromKeyedServices(nameof(AgentsOptions.AzureOpenAIDeployments.Reasoning))] IChatClient chatClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models, IPromptTemplateRenderer prompts,
        UserClaimsAIContextProvider userClaimsProvider,
        ConversationStoreChatHistoryProvider historyProvider) : base("PurposeResponder") {
        _options = options.Value;
        _model = _options.AzureOpenAI.Deployments.Reasoning!;

        var chatOptions = models.Value.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = prompts.Render("PurposeResponder", new {
            strictGrounding = _options.Pipeline.StrictGrounding,
        });

        _agent = chatClient
            .AsAIAgent(
                options: new ChatClientAgentOptions() {
                    ChatOptions = chatOptions,
                    AIContextProviders = [userClaimsProvider],
                    Name = "DexPurposeResponder",
                    ChatHistoryProvider = historyProvider,
                    // Chat completions is stateless; the echoed request ConversationId must not be treated as server-side history.
                    ThrowOnChatHistoryProviderConflict = false,
                });
    }

    /// <inheritdoc/>
    public override async ValueTask<GroundedAnswerOutput> HandleAsync(
        IntentOutput intentResult, IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        var state = await context.GetConversationStateAsync(cancellationToken);
        var prompt = state.Message.Text;
        var agentSession = await _agent.CreateSessionAsync(cancellationToken);
        ConversationStoreChatHistoryProvider.SetSessionId(agentSession, Guid.Parse(state.ConversationId));

        // Stream the answer: emit each text delta as a workflow event (surfaced as an SSE `delta` by the
        // streaming runner; ignored by the non-streaming runner) while accumulating the full text. Token
        // usage arrives as trailing UsageContent on the final update(s); fold it and report it once.
        var answer = new StringBuilder();
        await foreach (var update in _agent.RunStreamingAsync(prompt, agentSession, cancellationToken: cancellationToken)) {
            if (!string.IsNullOrEmpty(update.Text)) {
                answer.Append(update.Text);
            }
            await context.AddEventAsync(new AgentResponseUpdateEvent(Id, update), cancellationToken);
        }
        if(_options.DebugMode) {
            await context.AddEventAsync(new AgentResponseUpdateEvent(Id,
                                    new AgentResponseUpdate(ChatRole.Assistant, BuildContents())
                                ), cancellationToken);
        }
        return new GroundedAnswerOutput(answer.ToString(), [], []);

    }

    /// <summary>
    /// Helper method to display the rendering capabilities of the solution: one part per alternative media type the
    /// chat UI knows how to render.
    /// </summary>
    public List<AIContent> BuildContents() {
        var contents = new List<AIContent> {
            new TextContent("I can render text, images, callouts, confirmations, and multiple-choice prompts among others."),
            DataContentExtensions.JsonPart(new Callout {
                Severity = Callout.Severities.Warning,
                Title = "Outside my knowledge base",
                Text = "I answer only from the internal documentation loaded into this assistant, so anything beyond it I have to turn down."
            }, AgentsConstants.MediaTypes.Callout),
            // The same mark twice, captioned two ways: the envelope carries its caption in the payload, the bare
            // image/png part carries it as the part's name. Both render as the same figure.
            DataContentExtensions.JsonPart(
                ImageReference.FromBytes(_logo, "image/png", caption: "Dex answers from your knowledge base."),
                AgentsConstants.MediaTypes.Image),
            new DataContent(_logo, "image/png") { Name = "The same mark, carried as a bare image/png part." },
            new DataContent($"data:,{Uri.EscapeDataString("""
            <section class="dex-card">
              <img src="https://i.pravatar.cc/160?img=32" alt="Maria Papadopoulou" />
              <div>
                <h3>Maria Papadopoulou</h3>
                <p><span class="dex-badge">Customer Success</span> <span class="dex-muted">Support Department</span></p>
                <ul>
                  <li>📧 <a href="mailto:maria.papadopoulou@example.com">maria.papadopoulou@example.com</a></li>
                  <li>📞 <a href="tel:+302101234567">+30 210 123 4567</a></li>
                </ul>
              </div>
            </section>
            """)}", MediaTypeNames.Text.Html) { Name = "I can render a beautiful HTML fragment" },
            new DataContent($"data:,{Uri.EscapeDataString("""
           <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#04AA6D" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M20 6L9 17l-5-5"/>
            </svg>
           """)}", MediaTypeNames.Image.Svg){ Name = "I can also render a Checkmark SVG" },

            DataContentExtensions.JsonPart(new Confirmation {
                Prompt = "Want a hand finding something I do cover?",
                ConfirmText = "Yes, what can you help with?",
                CancelText = "No, thanks"
            }, AgentsConstants.MediaTypes.Confirmation)
        };
        var subjects = _options.Taxonomy.Categories.ToList();
        if (subjects.Count > 0) {
            contents.Add(DataContentExtensions.JsonPart(new MultipleChoice {
                Options = [.. subjects.Select(category => $"What can you tell me about {category}?")]
            }, AgentsConstants.MediaTypes.MultipleChoice));
        }
        return contents;
    }

    /// <summary>Reads the embedded sample image; empty when this assembly was built without it.</summary>
    private static ReadOnlyMemory<byte> ReadSampleLogo() {
        using var stream = typeof(PurposeResponder).Assembly.GetManifestResourceStream("Indice.Features.Agents.Core.Assets.dex-logo-128.png");
        if (stream is null) {
            return ReadOnlyMemory<byte>.Empty;
        }
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
