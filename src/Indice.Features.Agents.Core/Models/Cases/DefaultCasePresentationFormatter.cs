using System.Net;
using System.Text;

namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Default implementation of <see cref="ICasePresentationFormatter"/> for the current case schema.
/// </summary>
/// <remarks>
/// Expects case fields under:
/// <c>id</c>, <c>data.carPlate</c>, <c>data.email</c>, <c>data.phoneNumber</c>.
/// Register a custom formatter before <c>AddCasesWorkflow()</c> to support different schemas.
/// </remarks>
public class DefaultCasePresentationFormatter : ICasePresentationFormatter
{
    /// <inheritdoc/>
    public virtual CasePresentationResult Format(OtpValidationOutput input) {
        ArgumentNullException.ThrowIfNull(input);

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
        return new CasePresentationResult(answer, htmlCard);
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
