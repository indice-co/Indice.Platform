using System;
using System.Collections.Generic;
using System.Text;
using Indice.Features.Cases.Data.Models;

namespace Indice.Features.Cases.Models
{
    public class CheckpointTypeRequest
    {
        /// <summary>
        /// The name of the checkpoing
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The description of the checkpoint.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The public status of the checkpoint.
        /// </summary>
        public CasePublicStatus PublicStatus { get; set; }

        /// <summary>
        /// Boolean for whether the checkpoint is private.
        /// </summary>
        public bool Private { get; set; }
    }
}
