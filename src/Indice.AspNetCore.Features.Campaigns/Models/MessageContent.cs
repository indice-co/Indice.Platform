using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// Encapsulates the message content for a given <see cref="MessageDeliveryChannel"/>
    /// </summary>
    public class MessageContent
    {
        /// <summary>
        /// The title of the message
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The body of the message
        /// </summary>
        public string Body { get; set; }

    }
}
