using Indice.AspNetCore.Features.Campaigns.Models;

namespace Indice.AspNetCore.Features.Campaigns.Events
{
    /// <summary>
    /// The event model used when a contact is created or updated.
    /// </summary>
    public class UpsertContactEvent
    {
        /// <summary>
        /// Creates a new instance of <see cref="UpsertContactEvent"/>.
        /// </summary>
        /// <param name="recipientId">The id of the recipient.</param>
        /// <param name="isNew">Indicates whether the contact is new or already exists.</param>
        /// <param name="contact">The contact to create or update.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public UpsertContactEvent(string recipientId, bool isNew, Contact contact) {
            RecipientId = recipientId ?? throw new ArgumentNullException(nameof(recipientId));
            IsNew = isNew;
            Contact = contact ?? throw new ArgumentNullException(nameof(contact));
        }

        /// <summary>
        /// The id of the contact.
        /// </summary>
        public string RecipientId { get; }
        /// <summary>
        /// Indicates whether the contact is new or already exists.
        /// </summary>
        public bool IsNew { get; }
        /// <summary>
        /// Contact info to create or update.
        /// </summary>
        public Contact Contact { get; }
    }
}
