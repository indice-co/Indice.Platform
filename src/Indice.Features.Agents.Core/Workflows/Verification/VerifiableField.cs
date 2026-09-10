namespace Indice.Features.Agents.Core.Workflows.Verification;

/// <summary>The kind of a <see cref="VerifiableField"/>, i.e. what the value means and how it is compared.</summary>
public enum VerifiableFieldKind
{
    /// <summary>An e-mail address. Doubles as an OTP delivery channel.</summary>
    Email,
    /// <summary>A phone number. Doubles as an OTP delivery channel.</summary>
    Phone,
    /// <summary>A vehicle license/registration plate.</summary>
    LicensePlate,
    /// <summary>A physical (billing or delivery) address.</summary>
    PostalAddress,
    /// <summary>A tax identification number (VAT, AFM).</summary>
    TaxId,
    /// <summary>A customer code/number identifying the party in the source system.</summary>
    CustomerCode,
    /// <summary>A contract or account number.</summary>
    ContractNumber,
    /// <summary>A case or reference number.</summary>
    CaseNumber
}

/// <summary>
/// A single value inside a customer data payload that the person in the conversation can be challenged
/// against. Its <see cref="Value"/> never leaves the server: it is only ever compared with what the user
/// types, or — for the channel kinds — masked before being shown.
/// </summary>
/// <param name="Path">Dotted path of the value inside the payload, e.g. <c>customer.contactDetails.mobile</c>.</param>
/// <param name="Kind">What the value means.</param>
/// <param name="Value">The value as found in the payload.</param>
public record VerifiableField(string Path, VerifiableFieldKind Kind, string Value)
{
    /// <summary>Whether an one-time password can be delivered to this value.</summary>
    public bool IsChannel => Kind is VerifiableFieldKind.Email or VerifiableFieldKind.Phone;
}
