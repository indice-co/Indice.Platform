using System;

namespace Indice.Features.Messages.Core.Models.Requests
{
    /// <inheritdoc />
    public class CreateDistributionListContactRequest : CreateContactRequest
    {
        /// <summary>The id of the existing contact.</summary>
        public Guid? ContactId { get; set; }
    }
}
