using System;

namespace Indice.Features.Cases.Models
{
    public class CreateCaseResponse
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
    }
}