using System.Text;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Workflows.Events;
using Indice.Features.Agents.Core.Workflows.Mcp;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Composes the final grounded answer from the reranked candidates. Instructs the model to ground only
/// in the provided context and cite chunk IDs in <c>[#chunkId]</c> form. Projects the candidates into
/// <see cref="Models.Citation"/> records on the output payload.
/// </summary>
public sealed class IdentityAgent : Executor<PipelineStepContext<IntentOutput>, PipelineStepContext<RagPipelineOutput>>
{
    private readonly AzureOpenAIClient _openAIClient;
    private readonly AgentsOptions _options;
    private readonly ModelsOptions _models;
    private readonly IPromptTemplateRenderer _prompts;
    private readonly UserClaimsAIContextProvider _userClaimsProvider;
    private readonly IMcpToolsRegistry _mcpToolsRegistry;
    private readonly string _model;

    /// <summary>Creates a new <see cref="IdentityAgent"/>.</summary>
    public IdentityAgent(AzureOpenAIClient openAIClient, IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models, IPromptTemplateRenderer prompts,
        UserClaimsAIContextProvider userClaimsProvider, IMcpToolsRegistry mcpToolsRegistry) : base("IdentityAgent") {
        _openAIClient = openAIClient;
        _options = options.Value;
        _models = models.Value;
        _prompts = prompts;
        _userClaimsProvider = userClaimsProvider;
        _mcpToolsRegistry = mcpToolsRegistry;
        _model = _options.AzureOpenAI.Deployments.Reasoning!;
    }

    /// <inheritdoc/>
    public override async ValueTask<PipelineStepContext<RagPipelineOutput>> HandleAsync(PipelineStepContext<IntentOutput> envelope,
        IWorkflowContext context, CancellationToken cancellationToken = default) {

        var mcpTools = await _mcpToolsRegistry.GetToolsAsync(cancellationToken);

        var chatOptions = _models.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = _prompts.Render("IdentityTools", new {
            strictGrounding = _options.Pipeline.StrictGrounding,
        });
        if (mcpTools.Count > 0) {
            chatOptions.Tools = [.. (chatOptions.Tools ?? []), .. mcpTools];
        }

        var agent = _openAIClient
            .GetChatClient(_model)
            .AsAIAgent(
                options: new ChatClientAgentOptions() {
                    ChatOptions = chatOptions,
                    AIContextProviders = [_userClaimsProvider],
                    Name = "IdentityAgent"
                });

        var candidates = envelope.Payload.Intent;
        var prompt = BuildPrompt(envelope.State.Question);

        // Stream the answer: emit each text delta as a workflow event (surfaced as an SSE `delta` by the
        // streaming runner; ignored by the non-streaming runner) while accumulating the full text. Token
        // usage arrives as trailing UsageContent on the final update(s); fold it and report it once.
        var answer = new StringBuilder();
        UsageDetails? usage = null;
        await foreach (var update in agent.RunStreamingAsync(prompt, cancellationToken: cancellationToken)) {
            if (!string.IsNullOrEmpty(update.Text)) {
                answer.Append(update.Text);
                await context.AddEventAsync(new AnswerDeltaEvent(update.Text), cancellationToken);
            }
            foreach (var usageContent in update.Contents.OfType<UsageContent>()) {
                (usage ??= new UsageDetails()).Add(usageContent.Details);
            }
        }
        if (usage is not null) {
            await context.AddEventAsync(new UsageEvent(usage, _model), cancellationToken);
        }

        return envelope.Next(new RagPipelineOutput {
            Answer = answer.ToString()
        });
    }

    private static string BuildPrompt(string question) {
        var sb = new StringBuilder();
        sb.Append("QUESTION: ").AppendLine(question);
        return sb.ToString();
    }
}
}