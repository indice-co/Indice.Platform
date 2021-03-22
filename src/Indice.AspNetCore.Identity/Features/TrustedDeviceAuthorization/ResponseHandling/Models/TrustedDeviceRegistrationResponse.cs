namespace Indice.AspNetCore.Identity.Features
{
    internal class TrustedDeviceRegistrationResponse
    {
        public string UserId { get; }
        public string DeviceFriendlyName { get; }
        public byte[] Challenge { get; }
    }
}
