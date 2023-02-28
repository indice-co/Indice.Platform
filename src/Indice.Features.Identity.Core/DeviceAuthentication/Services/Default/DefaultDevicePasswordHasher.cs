using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Services;

/// <summary>Default implementation of <see cref="IDevicePasswordHasher"/> that relies on ASP.NET Identity's <see cref="PasswordHasher{TUser}"/> implementation.</summary>
public class DefaultDevicePasswordHasher : IDevicePasswordHasher
{
    /* https://andrewlock.net/exploring-the-asp-net-core-identity-passwordhasher/ */
    private readonly PasswordHasher<User> _passwordHasher;

    /// <summary>Creates a new instance of <see cref="DefaultDevicePasswordHasher"/>.</summary>
    /// <param name="passwordHasher">Implements the standard Identity password hashing.</param>
    public DefaultDevicePasswordHasher(PasswordHasher<User> passwordHasher) {
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    /// <inheritdoc />
    public string HashPassword(UserDevice device, string password) => _passwordHasher.HashPassword(null, password);

    /// <inheritdoc />
    public PasswordVerificationResult VerifyHashedPassword(UserDevice device, string hashedPassword, string providedPassword) => _passwordHasher.VerifyHashedPassword(null, hashedPassword, providedPassword);
}
