using System;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// Model used to filter the list of user messages.
    /// </summary>
    public class GetMessagesListFilter
    {
        /// <summary>
        /// The id of a campaign type.
        /// </summary>
        public Guid? TypeId { get; set; }
    }
}
