using System;
using System.Collections.Generic;
using Indice.Features.Cases.Data.Models;

namespace Indice.Features.Cases.Models.Responses
{
    public class CasePartial
    {
        public Guid Id { get; set; }
        public Guid CheckpointTypeId { get; set; }
        public CasePublicStatus PublicStatus { get; set; }
        public string CustomerId { get; set; }
        public string UserId { get; set; }
        public string CustomerName { get; set; }
        public DateTime? CreatedByWhen { get; set; }
        public CaseType? CaseType { get; set; }
        public Dictionary<string,string>? Metadata { get; set; }
        public string? GroupId { get; set; }
        public string? CheckpointTypeCode { get; set; }
        public string? Data { get; set; }
        public string? AssignedToName { get; set; }
        public string? Channel { get; set; }
        public bool Draft { get; set; }
    }
}
