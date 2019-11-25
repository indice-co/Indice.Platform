using System.ComponentModel.DataAnnotations;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a new user that will be created on the server.
    /// </summary>
    public class CreateUserRequest
    {
        /// <summary>
        /// The first name of the user.
        /// </summary>
        public string FirstName { get; set; }
        /// <summary>
        /// The last name of the user.
        /// </summary>
        public string LastName { get; set; }
        /// <summary>
        /// The username used to login.
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "Field 'UserName' is required.")]
        public string UserName { get; set; }
        /// <summary>
        /// The email of the user.
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// The initial password of the user.
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// User's phone number.
        /// </summary>
        public string PhoneNumber { get; set; }
    }
}
