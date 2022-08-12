using System;
using System.Collections.Generic;
using Indice.Features.Cases.Data.Models;

namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// The partial model of a case.
    /// </summary>
    public class CasePartial
    {
        /// <summary>
        /// The Id of the case.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The current checkpoint of the case.
        /// </summary>
        public Guid CheckpointTypeId { get; set; }

        /// <summary>
        /// The current public status of the case.
        /// </summary>
        public CasePublicStatus PublicStatus { get; set; }

        /// <summary>
        /// The Id of the customer as provided from integration services (core or 3rd party).
        /// </summary>
        public string CustomerId { get; set; }

        /// <summary>
        /// The Id of the user as provided from our Identity server.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The full name of the customer.
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// The created date of the case.
        /// </summary>
        public DateTime? CreatedByWhen { get; set; }

        /// <summary>
        /// The Id of the user that created the case.
        /// </summary>
        public string CreatedById { get; set; }

        /// <summary>
        /// The <see cref="CaseType"/> of the case.
        /// </summary>
        public CaseTypePartial? CaseType { get; set; }

        /// <summary>
        /// The case metadata as provided from the client or integrator.
        /// </summary>
        public Dictionary<string,string>? Metadata { get; set; }

        /// <summary>
        /// The Id of the group the case belongs.
        /// </summary>
        public string? GroupId { get; set; }

        /// <summary>
        /// The current checkpoint type code for the case.
        /// </summary>
        public string? CheckpointTypeCode { get; set; }

        /// <summary>
        /// The json data of the case.
        /// </summary>
        public string? Data { get; set; }

        /// <summary>
        /// The name of the user that has the case assigned.
        /// </summary>
        public string? AssignedToName { get; set; }

        /// <summary>
        /// The channel of th case.
        /// </summary>
        public string? Channel { get; set; }

        /// <summary>
        /// Indicate if the case is in draft mode.
        /// </summary>
        public bool Draft { get; set; }
    }
}
