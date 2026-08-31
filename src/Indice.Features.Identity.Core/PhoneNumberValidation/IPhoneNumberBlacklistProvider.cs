namespace Indice.Features.Identity.Core.PhoneNumberValidation;

/// <summary>
/// Represents a provider that supplies blacklisted phone numbers.
/// </summary>
public interface IPhoneNumberBlacklistProvider
{
    /// <summary>
    /// Determines whether the specified phone number is blacklisted.
    /// </summary>
    /// <param name="phoneNumber">The phone number to check.</param>
    /// <returns><see langword="true"/> if the phone number is blacklisted; otherwise, <see langword="false"/>.</returns>
    bool IsPhoneNumberBlacklisted(string phoneNumber);
}
