using System.Net.Mime;
using Indice.Features.Agents.Core.Services;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Presents selected case data after OTP verification completes.
/// </summary>
public sealed class DataPresenterStep : Executor<OtpValidationOutput, OperatorPipelineOutput>
{
    private readonly ICasePresentationFormatter _presentationFormatter;

    /// <summary>Creates a new <see cref="DataPresenterStep"/>.</summary>
    public DataPresenterStep(ICasePresentationFormatter presentationFormatter) : base(nameof(DataPresenterStep)) {
        _presentationFormatter = presentationFormatter;
    }

    /// <inheritdoc/>
    public override async ValueTask<OperatorPipelineOutput> HandleAsync(
        OtpValidationOutput input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        ArgumentNullException.ThrowIfNull(input);

        if (!input.IsValid) {
            await context.AddEventAsync(
                new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, [new TextContent(input.Message)])),
                cancellationToken);
            return new OperatorPipelineOutput { Answer = input.Message };
        }

        var presentation = _presentationFormatter.Format(input);

        await context.AddEventAsync(
            new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, [new DataContent($"data:,{Uri.EscapeDataString(presentation.HtmlCard)}", MediaTypeNames.Text.Html) { Name = "HTML Card" },])),
            cancellationToken);
        return new OperatorPipelineOutput { Answer = presentation.Answer };
    }
}

/// <summary>
/// Terminal output of any LLM agent step that streams a free-text answer to the caller.
/// </summary>
public sealed class OperatorPipelineOutput
{
    /// <summary>The fully accumulated answer text produced by the model.</summary>
    public string Answer { get; init; } = string.Empty;
}

