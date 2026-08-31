using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.Core.PhoneNumberValidation;

/// <summary>
/// Validates that the user's phone number is not included in any configured blacklist.
/// </summary>
public sealed class PhoneNumberBlacklistValidator : PhoneNumberBlacklistValidator<User>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PhoneNumberBlacklistValidator"/> class.
    /// </summary>
    /// <param name="providers">The phone number blacklist providers to use.</param>
    public PhoneNumberBlacklistValidator(IEnumerable<IPhoneNumberBlacklistProvider> providers)
        : base(providers) { }
}

/// <summary>
/// Validates that the user's phone number is not included in any configured blacklist.
/// </summary>
/// <typeparam name="TUser">The type of user instance.</typeparam>
public class PhoneNumberBlacklistValidator<TUser> : IUserValidator<TUser> where TUser : User
{
    private readonly IEnumerable<IPhoneNumberBlacklistProvider> _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhoneNumberBlacklistValidator{TUser}"/> class.
    /// </summary>
    /// <param name="providers">The phone number blacklist providers to use.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="providers"/> is <see langword="null"/>.
    /// </exception>
    public PhoneNumberBlacklistValidator(IEnumerable<IPhoneNumberBlacklistProvider> providers) {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
    }

    /// <inheritdoc/>
    public Task<IdentityResult> ValidateAsync(
        UserManager<TUser> manager,
        TUser user) {
        if (!IsBlacklisted(user.PhoneNumber)) {
            return Task.FromResult(IdentityResult.Success);
        }

        var errorDescriber = manager.ErrorDescriber as ExtendedIdentityErrorDescriber
            ?? new ExtendedIdentityErrorDescriber();

        return Task.FromResult(
            IdentityResult.Failed(
                errorDescriber.PhoneNumberBlacklisted(user.PhoneNumber)));
    }

    private bool IsBlacklisted(string? phoneNumber) {
        if (string.IsNullOrWhiteSpace(phoneNumber)) {
            return false;
        }

        if (!PhoneNumberNormalizer.TryNormalize(phoneNumber, out var normalized)) {
            return false;
        }

        return _providers.Any(x => x.IsPhoneNumberBlacklisted(normalized));
    }
}