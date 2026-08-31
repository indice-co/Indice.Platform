namespace Indice.Features.Identity.Core.PhoneNumberValidation;

/// <summary>
/// Provides utilities for normalizing phone numbers for blacklist lookups.
/// </summary>
internal static class PhoneNumberNormalizer
{
    /// <summary>
    /// Attempts to normalize a phone number to international format.
    /// </summary>
    /// <param name="phoneNumber">The phone number to normalize.</param>
    /// <param name="normalized">The normalized phone number.</param>
    /// <returns>
    /// <see langword="true"/> if the phone number was successfully normalized;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryNormalize(string? phoneNumber, out string normalized) {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(phoneNumber)) {
            return false;
        }

        Span<char> buffer = stackalloc char[phoneNumber.Length + 1];
        var index = 0;

        foreach (var character in phoneNumber) {
            if (character is >= '0' and <= '9') {
                buffer[index++] = character;
                continue;
            }

            if (character == '+' && index == 0) {
                buffer[index++] = character;
            }
        }

        if (index < 2) {
            return false;
        }

        if (buffer[0] == '+') {
            normalized = new string(buffer[..index]);
            return true;
        }

        if (buffer[..index].StartsWith("00")) {
            normalized = $"+{new string(buffer[2..index])}";
            return normalized.Length > 1;
        }

        return false;
    }
}