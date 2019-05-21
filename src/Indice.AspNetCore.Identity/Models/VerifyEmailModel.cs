namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Model used by views that required user email verification.
    /// </summary>
    public class VerifyEmailModel
    {
        /// <summary>
        /// The email of the user.
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Determines whether the login will be remembered after user closes browser window.
        /// </summary>
        public bool RememberLogin { get; set; }
    }
}
