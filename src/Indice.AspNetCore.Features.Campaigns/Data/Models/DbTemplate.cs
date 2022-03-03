using System;
using System.Collections.Generic;
using Indice.AspNetCore.Features.Campaigns.Models;

namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    internal class DbTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Dictionary<MessageDeliveryChannel, MessageContent> Content { get; set; }
    }
}
