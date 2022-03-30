using System;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <inheritdoc />
    public class CreateDistributionListContactRequest : CreateContactRequest
    {
        /// <summary>
        /// The id of the existing contact.
        /// </summary>
        public Guid? Id { get; set; }
    }
}
