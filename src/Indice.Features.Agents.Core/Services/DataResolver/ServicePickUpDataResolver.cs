using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Features.Agents.Core.Workflows.State;

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
    public const string ServiceKey = "ServicePickUp";

    /// <inheritdoc/>
    public virtual CustomerData Resolve(JsonNode caseData) {
        ArgumentNullException.ThrowIfNull(caseData);
        return new CustomerData {
            Data = caseData.Deserialize<JsonElement>(),
            DataType = caseData["caseType"]?["code"]?.GetValue<string>() ?? throw new InvalidOperationException("DataType not found in case data."),
            ReferenceId = caseData["id"]?.GetValue<string>() ?? throw new InvalidOperationException("ReferenceId not found in case data."),
            PhoneNumber = caseData["data"]["phoneNumber"]?.GetValue<string>(),
            Email = caseData["data"]["email"]?.GetValue<string>(),
            ChallengeValue = caseData["data"]["carPlate"]?.GetValue<string>(),
            ChallengeType = "carPlate"
        };
    }

    /// <inheritdoc/>
    public virtual OperationResult Validate(CustomerData data) {
        ArgumentNullException.ThrowIfNull(data);
        if (string.IsNullOrWhiteSpace(data.ChallengeValue))
            return OperationResult.Failure("Verification value is missing or empty.");

        if (string.IsNullOrWhiteSpace(data.PhoneNumber) && string.IsNullOrWhiteSpace(data.Email))
            return OperationResult.Failure("Phone number and email are missing or empty.");
        return OperationResult.Success();
    }
}
