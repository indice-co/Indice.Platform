using System.Net;
using System.Text;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Workflows.Mcp;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

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
            await context.AddEventAsync(
                new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, [new TextContent(input.Message)])),
                cancellationToken);
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

        var htmlCard = BuildCaseHtmlCard(input.Message, caseId, plate, email, phone);
        var dataUri = $"data:application/vnd.indice.html-card;charset=utf-8,{Uri.EscapeDataString(htmlCard)}";

        await context.AddEventAsync(
            new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, [new DataContent(dataUri, "application/vnd.indice.html-card")])),
            cancellationToken);
        return new RagPipelineOutput { Answer = answer };
    }

    private static string BuildCaseHtmlCard(string message, string caseId, string plate, string email, string phone) =>
        $"""
        <article class=\"rounded-box border border-base-300 bg-base-100 p-4 shadow-sm\">
          <h3 class=\"text-base font-semibold text-base-content\">{WebUtility.HtmlEncode(message)}</h3>
          <p>ABARTH Βελμάρ – Ν. Ερυθραία</p>
          <p>
            <img src="https://www.stock-center.gr/sites/default/files/styles/car_gallery/public/car/2026-08/RENAULT%20ARKANA%20XZB-9950%20%283%29.webp?itok=n0z71Awe" width="215" height="142">
            </p>
          <dl class=\"mt-3 grid gap-2 text-sm\">
            <dt class=\"inline text-base-content/60\">Case ID:</dt> <dd class=\"inline font-medium\">{WebUtility.HtmlEncode(caseId)}</dd>
            <dt class=\"inline text-base-content/60\">Plate:</dt> <dd class=\"inline font-medium\">{WebUtility.HtmlEncode(plate)}</dd>
            <dt class=\"inline text-base-content/60\">Email:</dt> <dd class=\"inline font-medium\">{WebUtility.HtmlEncode(email)}</dd>
            <dt class=\"inline text-base-content/60\">Phone:</dt> <dd class=\"inline font-medium\">{WebUtility.HtmlEncode(phone)}</dd>
          </dl>
        </article>
        """;
}
