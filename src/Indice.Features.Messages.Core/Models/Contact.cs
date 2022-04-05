namespace Indice.Features.Messages.Core.Models
{
    /// <summary>
    /// Models a contact in the system as a member of a distribution list.
    /// </summary>
    public class Contact
    {
        /// <summary>
        /// The unique id of the contact.
        /// </summary>
        public Guid Id { get; set; }
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
        /// <summary>
        /// Indicates when contact info were last updated.
        /// </summary>
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
