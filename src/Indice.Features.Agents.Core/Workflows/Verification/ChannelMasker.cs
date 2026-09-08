namespace Indice.Features.Agents.Core.Workflows.Verification;

/// <summary>
/// Masks the OTP delivery channels found in a customer data payload, so the assistant can say
/// <c>"we sent a code to +30 ••• ••• 4567"</c> without disclosing the number to whoever is on the line.
/// </summary>
public static class ChannelMasker
{
    /// <summary>Number of trailing characters left visible on a phone number.</summary>
    private const int PhoneVisibleDigits = 4;

    /// <summary>Masks <paramref name="field"/> for display. Non-channel kinds are fully masked.</summary>
    public static string Mask(VerifiableField field) => field.Kind switch {
        VerifiableFieldKind.Phone => MaskPhone(field.Value),
        VerifiableFieldKind.Email => MaskEmail(field.Value),
        _ => new string('•', 6)
    };

    /// <summary>Keeps the last <see cref="PhoneVisibleDigits"/> digits visible, e.g. <c>••••••4567</c>.</summary>
    public static string MaskPhone(string value) {
        var digits = new string([.. value.Where(char.IsAsciiDigit)]);
        if (digits.Length <= PhoneVisibleDigits) {
            return new string('•', digits.Length);
        }
        return new string('•', digits.Length - PhoneVisibleDigits) + digits[^PhoneVisibleDigits..];
    }

    /// <summary>Keeps the first character of the local part and the domain suffix visible, e.g. <c>m•••@•••.com</c>.</summary>
    public static string MaskEmail(string value) {
        var trimmed = value.Trim();
        var at = trimmed.IndexOf('@');
        if (at <= 0 || at == trimmed.Length - 1) {
            return new string('•', trimmed.Length);
        }
        var local = trimmed[..at];
        var domain = trimmed[(at + 1)..];
        var dot = domain.LastIndexOf('.');
        var suffix = dot > 0 ? domain[dot..] : string.Empty;
        return $"{local[0]}{new string('•', Math.Max(local.Length - 1, 1))}@{new string('•', 3)}{suffix}";
    }
}
