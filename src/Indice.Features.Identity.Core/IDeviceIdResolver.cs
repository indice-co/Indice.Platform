namespace Indice.Features.Identity.Core;

/// <summary>An abstraction used to specify the way to resolve the device identifier using various ways.</summary>
public interface IDeviceIdResolver
{
    /// <summary>Gets the device identifier.</summary>
    Task<MfaDeviceIdentifier> Resolve();
}
