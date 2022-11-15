using System;
using Indice.Features.Cases.Data.Models;
using Newtonsoft.Json.Converters;

namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// The checkpoint entry for a case.
    /// </summary>
    public class Checkpoint
    {
        /// <summary>
        /// The Id of the checkpoint.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// The checkpoint type code. This is the inner status the back-officer can see.
        /// </summary>
        public string CheckpointTypeCode { get; set; }
        
        /// <summary>
        /// The public status of the case. This is the external status for the customer.
        /// </summary>
        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))] // Because of Elsa.HttpActivities the default serializer is newtonsoft
        public CasePublicStatus PublicStatus { get; set; }
        
        /// <summary>
        /// The completed date of the checkpoint.
        /// </summary>
        public DateTimeOffset? CompletedDate { get; set; }
        
        /// <summary>
        /// Indicates if the checkpoint is private, which means not visible to the customer.
        /// </summary>
        public bool? Private { get; set; }

        /// <summary>
        /// The due date of the checkpoint.
        /// </summary>
        public DateTimeOffset? DueDate { get; set; }
    }
}