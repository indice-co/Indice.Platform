using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Indice.Features.Agents.Core.Workflows.Steps.Risk;

/// <summary>
/// Removes what must never leave the bank's databases from the evidence before it is put in a report or shown to a model: secrets
/// and one-time-password values, and (optionally) the full contact details of the customer. The risk event payload
/// (<c>RiskEvent.Data</c>) is documented to hold OTP codes, message bodies, full user claims and the initial secret of a simple user
/// (knowledge base L-13), so it is never used raw.
/// </summary>
internal sealed partial class RiskEvidenceRedactor
{
    /// <summary>The mask character used in place of removed digits and letters.</summary>
    public const char Mask = '•';

    // Whole property names that hold a secret. `tokenDeviceId` is a device id and stays; anything that ends in "token" goes.
    [GeneratedRegex(@"^(otp.*|.*otpverificationcode|secretcode|.*secret.*|password|newpassword|oldpassword|currentpassword|confirmpassword|passwordhash|securitystamp|pin|cvv2?|cvc|claims|publickey|privatekey)$|token$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SecretKey();

    [GeneratedRegex(@"^(email|e-?mail|phone.*|mobile.*|msisdn)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ContactKey();

    [GeneratedRegex(@"^(smsbody|emailbody|pushnotificationbody|body|message|text)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MessageBodyKey();

    // 4 to 8 digits standing alone: the usual shape of a one-time password. A figure like 1250.00 or 1,250 is left alone.
    [GeneratedRegex(@"(?<![\d.,])\d{4,8}(?![\d]|[.,]\d)", RegexOptions.CultureInvariant)]
    private static partial Regex OtpLike();

    private readonly bool _maskContactDetails;

    /// <summary>Creates a redactor.</summary>
    /// <param name="maskContactDetails">Whether phone numbers, e-mail addresses and tax ids are partially masked.</param>
    public RiskEvidenceRedactor(bool maskContactDetails) {
        _maskContactDetails = maskContactDetails;
    }

    /// <summary>Whether a property name holds a secret that is dropped from evidence.</summary>
    public static bool IsSecretKey(string key) => SecretKey().IsMatch(key);

    /// <summary>
    /// Replaces every run of four to eight digits with mask characters, because a message body that carries a one-time password must
    /// not be reproduced. Amounts written with decimals and numbers longer than eight digits (phone numbers, IBANs) are kept.
    /// </summary>
    public static string MaskOtp(string? text) =>
        string.IsNullOrEmpty(text) ? string.Empty : OtpLike().Replace(text, match => new string(Mask, match.Length));

    /// <summary>A phone number with its middle masked: <c>+30••••••678</c>. Unchanged when masking is off.</summary>
    public string? MaskPhone(string? phone) {
        if (!_maskContactDetails || string.IsNullOrWhiteSpace(phone)) {
            return phone;
        }
        var compact = phone.Replace(" ", string.Empty);
        return compact.Length <= 6 ? new string(Mask, compact.Length) : compact[..3] + new string(Mask, compact.Length - 6) + compact[^3..];
    }

    /// <summary>An e-mail address with most of the local part masked: <c>ko•••••@example.com</c>. Unchanged when masking is off.</summary>
    public string? MaskEmail(string? email) {
        if (!_maskContactDetails || string.IsNullOrWhiteSpace(email)) {
            return email;
        }
        var at = email.IndexOf('@');
        if (at <= 0) {
            return new string(Mask, Math.Min(email.Length, 8));
        }
        var local = email[..at];
        var kept = Math.Min(2, local.Length);
        return local[..kept] + new string(Mask, Math.Max(1, Math.Min(local.Length - kept, 6))) + email[at..];
    }

    /// <summary>A tax id or similar identifier with all but the last three characters masked. Unchanged when masking is off.</summary>
    public string? MaskIdentifier(string? value) {
        if (!_maskContactDetails || string.IsNullOrWhiteSpace(value)) {
            return value;
        }
        return value.Length <= 3 ? new string(Mask, value.Length) : new string(Mask, value.Length - 3) + value[^3..];
    }

    /// <summary>
    /// A redacted copy of a JSON payload: secret properties removed at any depth, one-time-password digits masked in message bodies, and
    /// contact properties masked when that is on. <c>null</c> when the text is empty or not JSON.
    /// </summary>
    public JsonNode? RedactJson(string? json) {
        if (string.IsNullOrWhiteSpace(json)) {
            return null;
        }
        try {
            return Redact(JsonNode.Parse(json), key: null);
        } catch (JsonException) {
            return null;
        }
    }

    private JsonNode? Redact(JsonNode? node, string? key) {
        switch (node) {
            case JsonObject obj: {
                var copy = new JsonObject();
                foreach (var (name, child) in obj) {
                    if (IsSecretKey(name)) {
                        continue;
                    }
                    copy[name] = Redact(child, name);
                }
                return copy;
            }
            case JsonArray array: {
                var copy = new JsonArray();
                foreach (var child in array) {
                    copy.Add(Redact(child, key));
                }
                return copy;
            }
            case JsonValue value when value.TryGetValue<string>(out var text) && key is not null:
                if (MessageBodyKey().IsMatch(key)) {
                    return JsonValue.Create(MaskOtp(text));
                }
                if (ContactKey().IsMatch(key)) {
                    return JsonValue.Create(key.Contains("mail", StringComparison.OrdinalIgnoreCase) ? MaskEmail(text) : MaskPhone(text));
                }
                return JsonValue.Create(text);
            case JsonValue value:
                return value.DeepClone();
            default:
                return null;
        }
    }
}
