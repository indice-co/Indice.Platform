namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>Request view model for the MFA login page.</summary>
    public class MfaLoginInputModel
    {
        /// <summary>The OTP code inserted by the user.</summary>
        public string OtpCode { get; set; }
        /// <summary>Flag that indicates that the device performed the login should be persisted.</summary>
        public bool RememberDevice { get; set; }
        /// <summary>The return URL after the login is successful.</summary>
        public string ReturnUrl { get; set; }
    }
}
