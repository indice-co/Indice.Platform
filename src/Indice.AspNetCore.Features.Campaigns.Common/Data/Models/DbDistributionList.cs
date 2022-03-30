namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    public class DbDistributionList
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public virtual ICollection<DbContact> Contacts { get; set; }
    }
}
