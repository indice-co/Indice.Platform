namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    public class DbMessageType
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
    }
}
