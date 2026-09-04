using System.Net.Mime;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Mcp;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Presents selected case data after OTP verification completes.
/// </summary>
public sealed class CasePresenterStep : Executor<OtpValidationOutput, RagPipelineOutput>
{
    private readonly ICasePresentationFormatter _presentationFormatter;
    private readonly ICasesReplayStateStore _stateStore;

    /// <summary>Creates a new <see cref="CasePresenterStep"/>.</summary>
    public CasePresenterStep(ICasePresentationFormatter presentationFormatter, ICasesReplayStateStore stateStore) : base(nameof(CasePresenterStep)) {
        _presentationFormatter = presentationFormatter;
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    /// <inheritdoc/>
    public override async ValueTask<RagPipelineOutput> HandleAsync(
        OtpValidationOutput input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        ArgumentNullException.ThrowIfNull(input);

        // Terminal step — the conversation's Cases state machine is finished.
        var conversationState = await context.GetConversationStateAsync(cancellationToken);
        await _stateStore.RemoveAsync(conversationState.ConversationId, cancellationToken);

        if (!input.IsValid) {
            await context.AddEventAsync(
                new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, [new TextContent(input.Message)])),
                cancellationToken);
            return new RagPipelineOutput { Answer = input.Message };
        }

        var presentation = _presentationFormatter.Format(input);

        await context.AddEventAsync(
            new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, [new DataContent($"data:,{Uri.EscapeDataString(presentation.HtmlCard)}", MediaTypeNames.Text.Html) { Name = "HTML Card" },])),
            cancellationToken);
        return new RagPipelineOutput { Answer = presentation.Answer };
    }
}
