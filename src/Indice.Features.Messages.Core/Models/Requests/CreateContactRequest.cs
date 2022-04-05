namespace Indice.Features.Messages.Core.Models.Requests
{
    /// <summary>
    /// The request model used to create a new contact.
    /// </summary>
    public class CreateContactRequest
    {
        /// <summary>
        /// The recipient correlation code.
        /// </summary>
        public string RecipientId { get; set; }
        /// <summary>
        /// Contact salutation (Mr, Mrs etc).
        /// </summary>
        public string Salutation { get; set; }
        /// <summary>
        /// The first name.
        /// </summary>
        public string FirstName { get; set; }
        /// <summary>
        /// The last name.
        /// </summary>
        public string LastName { get; set; }
        /// <summary>
        /// The full name.
        /// </summary>
        public string FullName { get; set; }
        /// <summary>
        /// The email.
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// The phone number.
        /// </summary>
        public string PhoneNumber { get; set; }
    }
}
