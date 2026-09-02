using System.Net.Mime;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Workflows.Mcp;
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

    /// <summary>Creates a new <see cref="CasePresenterStep"/>.</summary>
    public CasePresenterStep(ICasePresentationFormatter presentationFormatter) : base(nameof(CasePresenterStep)) {
        _presentationFormatter = presentationFormatter;
    }

    /// <inheritdoc/>
    public override async ValueTask<RagPipelineOutput> HandleAsync(
        OtpValidationOutput input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        ArgumentNullException.ThrowIfNull(input);

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
