using System.Text.Json.Nodes;

namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Default implementation of <see cref="ICaseDataExtractor"/> that reads the standard
/// field layout returned by the Indice Cases MCP service:
/// <c>id</c>, <c>data.phoneNumber</c>, <c>data.email</c>, and <c>data.carPlate</c>.
/// </summary>
/// <remarks>
/// To use a different JSON schema register your own <see cref="ICaseDataExtractor"/>
/// implementation in the DI container <em>before</em> calling <c>AddCasesWorkflow()</c>:
/// <code>
/// services.AddTransient&lt;ICaseDataExtractor, MyCaseDataExtractor&gt;();
/// services.AddCasesWorkflow();
/// </code>
/// </remarks>
public class DefaultCaseDataExtractor : ICaseDataExtractor
{
    /// <inheritdoc/>
    public virtual string ExtractCaseId(JsonNode caseData) =>
        caseData["id"]?.GetValue<string>()
            ?? throw new InvalidOperationException("CaseId not found in case data.");

    /// <inheritdoc/>
    public virtual string? ExtractPhoneNumber(JsonNode caseData) =>
        caseData["data"]?["phoneNumber"]?.GetValue<string>();

    /// <inheritdoc/>
    public virtual string? ExtractEmail(JsonNode caseData) =>
        caseData["data"]?["email"]?.GetValue<string>();

    /// <inheritdoc/>
    public virtual string? ExtractVerificationValue(JsonNode caseData) =>
        caseData["data"]?["carPlate"]?.GetValue<string>();
}
