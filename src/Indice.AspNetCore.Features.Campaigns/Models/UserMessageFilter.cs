using System;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// Model used to filter the list of user messages.
    /// </summary>
    public class UserMessageFilter
    {
        /// <summary>
        /// The id of a campaign type.
        /// </summary>
        public Guid[] TypeId { get; set; } = Array.Empty<Guid>();
        /// <summary>
        /// Active from.
        /// </summary>
        public DateTimeOffset? ActiveFrom { get; set; }
        /// <summary>
        /// Active to.
        /// </summary>
        public DateTimeOffset? ActiveTo { get; set; }
        /// <summary>
        /// Controls whether to show expired messages (that is outside of active period). Defaults to false.
        /// </summary>
        public bool? ShowExpired { get; set; } = false;
    }
}
