using System;

namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    internal class DbTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DbContent Content { get; set; }
    }
}
