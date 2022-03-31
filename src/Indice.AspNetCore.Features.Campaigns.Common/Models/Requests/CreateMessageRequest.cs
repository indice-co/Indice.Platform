namespace Indice.AspNetCore.Features.Campaigns.Models
{
    public class CreateMessageRequest
    {
        public Guid Id { get; set; }
        public string RecipientId { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public Guid CampaignId { get; set; }
    }
}
