using System.Text.Json.Serialization;
using Indice.Features.Agents.Core.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Services;

/// <summary>LLM-based first-turn router that selects which workflow should execute.</summary>
public sealed class LlmWorkflowRouter : IWorkflowRouter
{
    private const string ClarificationPromptDefault = "I can help with either knowledge questions or case-specific requests. Which one do you want?";

    private readonly AIAgent _agent;

    /// <summary>Creates a new <see cref="LlmWorkflowRouter"/>.</summary>
    public LlmWorkflowRouter(
        [FromKeyedServices(nameof(AgentsOptions.AzureOpenAIDeployments.Reasoning))] IChatClient chatClient,
        IOptions<ModelsOptions> models) {
        var chatOptions = models.Value.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = """
            You are a workflow router. Decide whether the user input should be handled by:
            - knowledge: general/product/documentation/faq questions.
            - cases: case-specific support requests that involve case details, ownership checks, or OTP verification.

            Return JSON with fields:
            - workflowName: one of [\"knowledge\", \"cases\"] when confident, otherwise null.
            - confidence: number from 0 to 1.
            - reason: short explanation.
            - isAmbiguous: true when confidence is low or intent unclear.
            - clarificationPrompt: short question asking user to choose when ambiguous.

            Be strict: if uncertain, set isAmbiguous=true.
            """;
        _agent = chatClient.AsAIAgent(new ChatClientAgentOptions {
            Name = "DexWorkflowRouter",
            ChatOptions = chatOptions
        });
    }

    /// <inheritdoc/>
    public async Task<WorkflowRoutingDecision> RouteAsync(ChatMessage message, CancellationToken cancellationToken = default) {
        var input = message.Text?.Trim();
        if (string.IsNullOrWhiteSpace(input)) {
            return new WorkflowRoutingDecision {
                WorkflowName = AgentsConstants.AgentNames.Knowledge,
                Confidence = 0,
                IsAmbiguous = true,
                Reason = "Empty input.",
                ClarificationPrompt = ClarificationPromptDefault,
                ClarificationOptions = [AgentsConstants.AgentNames.Knowledge, AgentsConstants.AgentNames.Cases]
            };
        }

        var session = await _agent.CreateSessionAsync(cancellationToken);
        var response = await _agent.RunAsync<WorkflowRouterResult>(input, session, cancellationToken: cancellationToken);
        var result = response.Result ?? new WorkflowRouterResult();
        var routedWorkflow = (result.WorkflowName ?? string.Empty).Trim().ToLowerInvariant();
        var knownWorkflow = routedWorkflow switch {
            AgentsConstants.AgentNames.Cases => AgentsConstants.AgentNames.Cases,
            AgentsConstants.AgentNames.Knowledge => AgentsConstants.AgentNames.Knowledge,
            _ => AgentsConstants.AgentNames.Knowledge
        };
        var isAmbiguous = result.IsAmbiguous || routedWorkflow is not (AgentsConstants.AgentNames.Knowledge or AgentsConstants.AgentNames.Cases);

        return new WorkflowRoutingDecision {
            WorkflowName = knownWorkflow,
            Confidence = double.Clamp(result.Confidence, 0, 1),
            IsAmbiguous = isAmbiguous,
            Reason = result.Reason,
            ClarificationPrompt = string.IsNullOrWhiteSpace(result.ClarificationPrompt) ? ClarificationPromptDefault : result.ClarificationPrompt,
            ClarificationOptions = [AgentsConstants.AgentNames.Knowledge, AgentsConstants.AgentNames.Cases]
        };
    }

    private sealed class WorkflowRouterResult
    {
        [JsonPropertyName("workflowName")]
        public string? WorkflowName { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("isAmbiguous")]
        public bool IsAmbiguous { get; set; }

        [JsonPropertyName("clarificationPrompt")]
        public string? ClarificationPrompt { get; set; }
    }
}
