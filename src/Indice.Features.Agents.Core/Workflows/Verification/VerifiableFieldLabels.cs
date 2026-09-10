namespace Indice.Features.Agents.Core.Workflows.Verification;

/// <summary>User-facing wording for the kinds a person can be challenged on.</summary>
public static class VerifiableFieldLabels
{
    /// <summary>
    /// The order challenges are offered in: attributes the caller must know from outside the conversation come
    /// first, delivery channels last, since a channel doubles as the OTP destination.
    /// </summary>
    public static readonly IReadOnlyList<VerifiableFieldKind> ChallengePriority = [
        VerifiableFieldKind.LicensePlate,
        VerifiableFieldKind.CustomerCode,
        VerifiableFieldKind.ContractNumber,
        VerifiableFieldKind.TaxId,
        VerifiableFieldKind.PostalAddress,
        VerifiableFieldKind.Phone,
        VerifiableFieldKind.Email
    ];

    /// <summary>Rank of <paramref name="kind"/> in <see cref="ChallengePriority"/>; unranked kinds sort last.</summary>
    public static int Priority(VerifiableFieldKind kind) {
        for (var index = 0; index < ChallengePriority.Count; index++) {
            if (ChallengePriority[index] == kind) {
                return index;
            }
        }
        return int.MaxValue;
    }

    /// <summary>Describes <paramref name="kind"/> the way the assistant asks for it.</summary>
    public static string Describe(VerifiableFieldKind kind) => kind switch {
        VerifiableFieldKind.Email => "your e-mail address",
        VerifiableFieldKind.Phone => "your phone number",
        VerifiableFieldKind.LicensePlate => "your license plate",
        VerifiableFieldKind.PostalAddress => "your delivery or billing address",
        VerifiableFieldKind.TaxId => "your tax id",
        VerifiableFieldKind.CustomerCode => "your customer code",
        VerifiableFieldKind.ContractNumber => "your contract number",
        VerifiableFieldKind.CaseNumber => "your case number",
        _ => "one of your details"
    };

    /// <summary>Joins <paramref name="kinds"/> into an "a, b or c" enumeration.</summary>
    public static string Describe(IEnumerable<VerifiableFieldKind> kinds) {
        var descriptions = kinds.Select(Describe).ToList();
        return descriptions.Count switch {
            0 => string.Empty,
            1 => descriptions[0],
            _ => $"{string.Join(", ", descriptions.Take(descriptions.Count - 1))} or {descriptions[^1]}"
        };
    }
}
