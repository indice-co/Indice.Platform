using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Features.Cases.Core.Models;

/// <summary>
/// Represent an aproval dto
/// </summary>
public class CaseApproval 
{ 
    /// <summary>Approval id</summary>
    public Guid Id { get; set; }

    /// <summary>Approval given by</summary>
    public AuditMeta CreatedBy { get; set; } = null!;

    /// <summary>Approval action</summary>
    public Approval Action { get; set; }
    
    /// <summary>An indicator whether the approval action is committed from user and the upcoming service. </summary>
    public bool Committed { get; set; }
    
    /// <summary>the reason of the approval action</summary>
    public string? Reason { get; set; }
}