namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a request for changing the username.
    /// </summary>
    public class ChangeUserNameRequest
    {
        /// <summary>
        /// The new username.
        /// </summary>
        public string UserName { get; set; }
    }
}
