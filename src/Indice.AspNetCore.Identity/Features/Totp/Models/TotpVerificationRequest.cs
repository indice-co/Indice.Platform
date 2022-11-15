using System.ComponentModel.DataAnnotations;

namespace Indice.AspNetCore.Identity.Features.Totp.Models
{
    /// <summary>Verification request object.</summary>
    public class TotpVerificationRequest
    {
        /// <summary>The TOTP code.</summary>
        [Required]
        public string Code { get; set; }
        /// <summary>Optionally pass the reason used to generate the TOTP.</summary>
        public string Purpose { get; set; }
    }
}
