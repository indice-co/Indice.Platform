namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// A data model containing the relationship between a user and a standard long-lived OTP code that can be used mostly for development purposes.
    /// </summary>
    public class UserTotp
    {
        /// <summary>
        /// The id of the user.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The generated OTP code.
        /// </summary>
        public string Code { get; set; }
    }
}
