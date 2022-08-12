using System.ComponentModel.DataAnnotations;
using Indice.Services;

namespace Indice.AspNetCore.Identity.Features.Totp.Models
{
    /// <summary>Verification request object.</summary>
    public class TotpVerificationRequest
    {
        /// <summary>The TOTP code.</summary>
        [Required]
        public string Code { get; set; }
        /// <summary>Optionally pass the provider to use to verify. Defaults to DefaultPhoneProvider.</summary>
        public TotpProviderType? Provider { get; set; }
        /// <summary>Optionally pass the reason used to generate the TOTP.</summary>
        public string Purpose { get; set; }
    }
}
