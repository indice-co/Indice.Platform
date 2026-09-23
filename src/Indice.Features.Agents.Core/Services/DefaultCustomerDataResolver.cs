using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Features.Agents.Core.Services;

namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Default implementation of <see cref="ICustomerDataResolver"/> that reads the standard
/// field layout returned by the Indice Cases MCP service:
/// <c>id</c>, <c>data.phoneNumber</c>, <c>data.email</c>, and <c>data.carPlate</c>.
/// </summary>
/// <remarks>
/// To use a different JSON schema register your own <see cref="ICustomerDataResolver"/>
/// implementation in the DI container <em>before</em> calling <c>AddCasesWorkflow()</c>:
/// <code>
/// services.AddTransient&lt;ICustomerDataResolver, MyCustomerDataResolver&gt;();
/// services.AddCasesWorkflow();
/// </code>
/// </remarks>
public class DefaultCustomerDataResolver : ICustomerDataResolver
{
    /// <inheritdoc/>
    public virtual string ExtractCaseId(JsonElement caseData) =>
        caseData.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("ReferenceId not found in case data.");

    /// <inheritdoc/>
    public virtual string? ExtractPhoneNumber(JsonElement caseData) =>
        caseData.GetProperty("data").GetProperty("phoneNumber").GetString();

    /// <inheritdoc/>
    public virtual string? ExtractEmail(JsonElement caseData) =>
        caseData.GetProperty("data").GetProperty("email").GetString();

    /// <inheritdoc/>
    public virtual string? ExtractChallengeValue(JsonElement caseData) =>
        caseData.GetProperty("data").GetProperty("carPlate").GetString();


    /// <inheritdoc/>
    public virtual OperationResult Validate(JsonElement caseData) {
        var verificationValue = ExtractChallengeValue(caseData);
        if (string.IsNullOrWhiteSpace(verificationValue))
            return OperationResult.Failure("Verification value is missing or empty.");

        if (string.IsNullOrWhiteSpace(ExtractPhoneNumber(caseData)) && string.IsNullOrWhiteSpace(ExtractEmail(caseData)))
            return OperationResult.Failure("Phone number and email are missing or empty.");
        return OperationResult.Success();
    }

    /// <inheritdoc/>
    public string ExtractDataType(JsonElement caseData) {
        return caseData.GetProperty("caseType").GetProperty("code").GetString()
            ?? throw new InvalidOperationException("DataType not found in case data.");
    }
}
