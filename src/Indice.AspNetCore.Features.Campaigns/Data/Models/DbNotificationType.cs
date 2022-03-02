using System;

namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    internal class DbNotificationType
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
    }
}
