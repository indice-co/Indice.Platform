using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Models.Requests;

/// <summary>
/// The model for creating a new checkpoint type
/// </summary>
public class CheckpointTypeRequest
{
    /// <summary>
    /// Id of the case type
    /// </summary>
    public Guid CaseTypeId { get; set; }
    /// <summary>
    /// Code of the checkpoint
    /// </summary>
    public string Code { get; set; }
    /// <summary>
    /// Ttitle of the checkpoint
    /// </summary>
    public string Title { get; set; }
    /// <summary>
    /// Description of the checkpoint
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// Translations of the checkpoint
    /// </summary>
    public string Translations { get; set; }
    /// <summary>
    /// The case status of the checkpoint
    /// </summary>
    public CaseStatus Status { get; set; }
    /// <summary>
    /// Flag for the checkpoint to denote if its private or not
    /// </summary>
    public bool Private { get; set; }
}
