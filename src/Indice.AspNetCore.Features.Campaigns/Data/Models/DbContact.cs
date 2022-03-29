using System;

namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    internal class DbContact
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string RecipientId { get; set; }
        public string Salutation { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string DeviceId { get; set; }
        public Guid? DistributionListId { get; set; }
    }
}
