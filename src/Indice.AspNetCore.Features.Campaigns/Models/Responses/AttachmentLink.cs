using System;
using Indice.Extensions;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// Models an attachment that is associated with a campaign.
    /// </summary>
    public class AttachmentLink
    {
        /// <summary>
        /// The id of the attachment.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The unique file identifier.
        /// </summary>
        public Guid FileGuid { get; set; } // TODO: Consider removing it.
        /// <summary>
        /// The URL to the file.
        /// </summary>
        public string PermaLink { get; set; }
        /// <summary>
        /// The label of the file.
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// The file size in bytes.
        /// </summary>
        public long Size { get; set; }
        /// <summary>
        /// The file size in readable format.
        /// </summary>
        public string SizeText => Size.ToFileSize();
        /// <summary>
        /// The content type of the file.
        /// </summary>
        public string ContentType { get; set; }
    }
}
