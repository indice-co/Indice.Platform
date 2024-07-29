using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Features.Cases.Models.Requests;

/// <summary>
/// The model for editing a checkpoint type
/// </summary>
public class EditCheckpointTypeRequest
{
    /// <summary>
    /// Id of the checkpoint type
    /// </summary>
    public Guid CheckpointTypeId { get; set; }
    /// <summary>
    /// Code of the check point type
    /// </summary>
    public string Code { get; set; }
    /// <summary>
    /// Id of the case type
    /// </summary>
    public Guid CaseTypeId { get; set; }
}
