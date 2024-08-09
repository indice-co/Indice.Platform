using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Features.Cases.Models.Requests;
/// <summary>
/// Request Dto to add a stakeholder to a case
/// </summary>
/// <param name="CaseId">The id of the case</param>
/// <param name="StakeHolderId">the Id of the stakeholder</param>
/// <param name="Type">The type of the stakeholder</param>
/// <param name="Accesslevel">Access level</param>
public record StakeHolderRequest(Guid CaseId, string StakeHolderId, byte Type, int Accesslevel);


/// <summary>
/// Request Dto to remove a stakeholder from a case
/// </summary>
/// <param name="CaseId">The id of the case</param>
/// <param name="StakeHolderId">the Id of the stakeholder</param>
/// <param name="Type">The type of the stakeholder</param>
public record StakeHolderDeleteRequest(Guid CaseId, string StakeHolderId, byte Type);