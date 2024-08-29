using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Features.Cases.Models.Requests;
/// <summary>
/// Request Dto to add a member to a case
/// </summary>
/// <param name="CaseId">The id of the case</param>
/// <param name="MemberId">the Id of the member</param>
/// <param name="Type">The type of the member</param>
/// <param name="Accesslevel">Access level</param>
public record CaseMemberRequest(Guid CaseId, string MemberId, byte Type, int Accesslevel);


/// <summary>
/// Request Dto to remove a member from a case
/// </summary>
/// <param name="CaseId">The id of the case</param>
/// <param name="CaseMemberId">the Id of the member</param>
/// <param name="Type">The type of the member</param>
public record CaseMemberDeleteRequest(Guid CaseId, string CaseMemberId, byte Type);