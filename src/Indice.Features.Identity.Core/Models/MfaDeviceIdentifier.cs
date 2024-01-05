namespace Indice.Features.Identity.Core.Models;

/// <summary>Models an MFA device identifier.</summary>
/// <param name="Value">The device id.</param>
/// <param name="RegistrationId">The device registration id.</param>
public record MfaDeviceIdentifier(string Value, Guid? RegistrationId = null)
{
    /// <summary>Determines if there is a value for <see cref="RegistrationId"/>.</summary>
    public bool HasRegistrationId => RegistrationId.HasValue;
}
