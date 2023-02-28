using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Services;

/// <summary>Provides an abstraction for hashing passwords.</summary>
public interface IDevicePasswordHasher
{
    /// <summary>Returns a hashed representation of the supplied <paramref name="password"/> for the specified <paramref name="device"/>.</summary>
    /// <param name="device">The device whose password is to be hashed.</param>
    /// <param name="password">The password to hash.</param>
    /// <returns>A hashed representation of the supplied <paramref name="password"/> for the specified <paramref name="device"/>.</returns>
    string HashPassword(UserDevice device, string password);
    /// <summary>Returns a <see cref="PasswordVerificationResult"/> indicating the result of a password hash comparison.</summary>
    /// <param name="device">The device whose password should be verified.</param>
    /// <param name="hashedPassword">The hash value for a device's stored password.</param>
    /// <param name="providedPassword">The password supplied for comparison.</param>
    /// <returns>A <see cref="PasswordVerificationResult"/> indicating the result of a password hash comparison.</returns>
    /// <remarks>Implementations of this method should be time consistent.</remarks>
    PasswordVerificationResult VerifyHashedPassword(UserDevice device, string hashedPassword, string providedPassword);
}
