using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Features.Cases.Models.Responses;
/// <summary>The stakeHolder dto.</summary>
public class StakeHolder
{
    /// <summary>The Id of the stakeholder.</summary>
    public Guid Id { get; set; }
    /// <summary>The Id of the case.</summary>
    public Guid CaseId { get; set; }
    /// <summary>The Id of the persona.</summary>
    public string StakeHolderId { get; set; }
    /// <summary>The type  of the persona.</summary>
    public byte Type { get; set; }
    /// <summary>The access level given.</summary>
    public int Accesslevel { get; set; }
    /// <summary>When the record was created.</summary>
    public DateTimeOffset DateInserted { get; set; }
}