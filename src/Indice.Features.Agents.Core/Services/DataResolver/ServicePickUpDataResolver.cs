using System.Text.Json.Nodes;

namespace Indice.Features.Agents.Core.Services.DataResolver;

/// <summary>
/// Default implementation of <see cref="ICustomerDataResolver"/> that reads the standard
/// field layout returned by the Indice Cases MCP service:
/// <c>id</c>, <c>data.phoneNumber</c>, <c>data.email</c>, and <c>data.carPlate</c>.
/// </summary>
/// <remarks>
/// To use a different JSON schema for a specific case type register your own <see cref="ICustomerDataResolver"/>
/// implementation as a keyed service using the case type code as the key:
/// <code>
/// services.AddKeyedTransient&lt;ICustomerDataResolver, MyCustomerDataResolver&gt;("&lt;caseTypeCode&gt;");
/// </code>
/// Case types without a registered resolver fall back to this default implementation.
/// </remarks>
public class ServicePickUpDataResolver : ICustomerDataResolver
{
    /// <summary>The service key under which the default resolver is registered.</summary>
    public const string DefaultKey = "default";

    /// <inheritdoc/>
    public virtual string ExtractCaseId(JsonNode caseData) =>
        caseData?["id"]?.GetValue<string>()
            ?? throw new InvalidOperationException("ReferenceId not found in case data.");

    /// <inheritdoc/>
    public virtual string? ExtractPhoneNumber(JsonNode caseData) =>
        caseData?["phoneNumber"]?.GetValue<string>();

    /// <inheritdoc/>
    public virtual string? ExtractEmail(JsonNode caseData) =>
        caseData?["email"]?.GetValue<string>();

    /// <inheritdoc/>
    public virtual string? ExtractChallengeValue(JsonNode caseData) =>
        caseData?["carPlate"]?.GetValue<string>();


    /// <inheritdoc/>
    public virtual OperationResult Validate(JsonNode caseData) {
        var verificationValue = ExtractChallengeValue(caseData);
        if (string.IsNullOrWhiteSpace(verificationValue))
            return OperationResult.Failure("Verification value is missing or empty.");

        if (string.IsNullOrWhiteSpace(ExtractPhoneNumber(caseData)) && string.IsNullOrWhiteSpace(ExtractEmail(caseData)))
            return OperationResult.Failure("Phone number and email are missing or empty.");
        return OperationResult.Success();
    }

    /// <inheritdoc/>
    public string ExtractDataType(JsonNode caseData) {
        return caseData?["caseType"]?["code"]?.GetValue<string>()
            ?? throw new InvalidOperationException("DataType not found in case data.");
    }
}
