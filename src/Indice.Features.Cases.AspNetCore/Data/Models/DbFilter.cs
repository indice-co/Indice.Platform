using System;

namespace Indice.Features.Cases.Data.Models
{
    public class DbFilter
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string QueryParameters { get; set; }
    }
}