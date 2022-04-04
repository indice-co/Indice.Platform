using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.AspNetCore.Features.Campaigns.Services;

namespace Indice.AspNetCore.Features.Campaigns
{
    /// <summary>
    /// Job handler for <see cref="UpsertContactEvent"/>.
    /// </summary>
    public class UpsertContactHandler : ICampaignJobHandler<UpsertContactEvent>
    {
        /// <summary>
        /// Creates a new instance of <see cref="UpsertContactHandler"/>.
        /// </summary>
        /// <param name="contactService">A service that contains contact related operations.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public UpsertContactHandler(IContactService contactService) {
            ContactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
        }

        private IContactService ContactService { get; }

        /// <summary>
        /// Decides whether to create or update a contact in the system.
        /// </summary>
        /// <param name="event">The event model used when a contact is created or updated.</param>
        public async Task Process(UpsertContactEvent @event) {
            if (@event.IsNew) {
                await ContactService.Create(Mapper.ToCreateContactRequest(@event.Contact));
            } else {
                await ContactService.Update(@event.Contact.Id, Mapper.ToUpdateContactRequest(@event.Contact));
            }
        }
    }
}
