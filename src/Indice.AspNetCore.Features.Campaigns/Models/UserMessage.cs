using System;
using System.Text.Json.Serialization;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// Models a user message.
    /// </summary>
    public class UserMessage
    {
        /// <summary>
        /// The unique identifier of the user message.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The title of the user message.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The content of the user message.
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsRead { get; set; }
        public string ActionText { get; set; }
        public string ActionUrl { get; set; }
        public string AttachmentUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        [JsonIgnore]
        public bool IsDeleted { get; set; }
        [JsonIgnore]
        public bool IsActive { get; set; }
    }
}
