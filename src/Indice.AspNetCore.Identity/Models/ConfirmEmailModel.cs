namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Model used by views that required user email confirmation.
    /// </summary>
    public class ConfirmEmailModel
    {
        /// <summary>
        /// The URL to return.
        /// </summary>
        public string ReturnUrl { get; set; }
        /// <summary>
        /// Determines whether the login will be remembered after user closes browser window.
        /// </summary>
        public bool RememberLogin { get; set; }
    }
}
