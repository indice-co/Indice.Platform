using Indice.AspNetCore.Features.Campaigns.Models;

namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    public class DbTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Dictionary<MessageDeliveryChannel, MessageContent> Content { get; set; }
    }
}
