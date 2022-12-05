using System;

namespace Indice.Features.Cases.Data.Models
{
    public class DbQuery
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string FriendlyName { get; set; }
        public string Parameters { get; set; }
    }
}