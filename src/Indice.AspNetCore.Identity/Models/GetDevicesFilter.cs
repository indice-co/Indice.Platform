namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>Filter options for querying devices.</summary>
    public class GetDevicesFilter
    {
        /// <summary>Creates a new instance of <see cref="GetDevicesFilter"/>.</summary>
        public GetDevicesFilter(bool? isTrusted = null, bool? isPendingTrustActivation = null) {
            IsTrusted = isTrusted;
            IsPendingTrustActivation = isPendingTrustActivation;
        }

        /// <summary>Indicates whether the device is a trusted device.</summary>
        public bool? IsTrusted { get; }
        /// <summary>Determines whether the device is pending trust activation.</summary>
        public bool? IsPendingTrustActivation { get; }
    }
}
