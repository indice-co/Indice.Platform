namespace Indice.AspNetCore.Features.Campaigns.Events
{
    /// <summary>
    /// The event model used when a contact is resolved from an external system.
    /// </summary>
    public class ContactResolutionEvent
    {
        /// <summary>
        /// Creates a new instance of <see cref="ContactResolutionEvent"/>.
        /// </summary>
        /// <param name="recipientId">The id of the recipient.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ContactResolutionEvent(string recipientId) {
            RecipientId = recipientId ?? throw new ArgumentNullException(nameof(recipientId));
        }

        /// <summary>
        /// The id of the contact.
        /// </summary>
        public string RecipientId { get; }
    }
}
