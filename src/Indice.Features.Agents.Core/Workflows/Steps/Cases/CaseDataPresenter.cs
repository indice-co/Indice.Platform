using System.Text;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Workflows.Mcp;
using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Presents selected case data after OTP verification completes.
/// </summary>
public sealed class CaseDataPresenter : Executor<OtpValidationOutput, RagPipelineOutput>
{
    /// <summary>Creates a new <see cref="CaseDataPresenter"/>.</summary>
    public CaseDataPresenter() : base(nameof(CaseDataPresenter)) {
    }

    /// <inheritdoc/>
    public override async ValueTask<RagPipelineOutput> HandleAsync(
        OtpValidationOutput input,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        ArgumentNullException.ThrowIfNull(input);

        if (!input.IsValid) {
            await context.AddEventAsync(new AnswerDeltaEvent(input.Message), cancellationToken);
            return new RagPipelineOutput { Answer = input.Message };
        }

        var caseData = input.OtpResponse.Challenge.ValidationData.OwnershipVerificationData.CaseRetrievalData.CaseData;
        var data = caseData?["data"];

        var caseId = input.OtpResponse.Challenge.CaseId;
        var plate = data?["carPlate"]?.GetValue<string>() ?? "-";
        var email = data?["email"]?.GetValue<string>() ?? "-";
        var phone = data?["phoneNumber"]?.GetValue<string>() ?? "-";

        var answer = new StringBuilder()
            .AppendLine(input.Message)
            .AppendLine($"Case ID: {caseId}")
            .AppendLine($"Plate: {plate}")
            .AppendLine($"Email: {email}")
            .AppendLine($"Phone: {phone}")
            .ToString();

        await context.AddEventAsync(new AnswerDeltaEvent(answer), cancellationToken);
        return new RagPipelineOutput { Answer = answer };
    }
}
