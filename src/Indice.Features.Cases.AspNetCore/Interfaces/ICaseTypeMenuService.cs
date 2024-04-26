using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Interfaces;
/// <summary>
/// Drives the menu service for case types
/// </summary>
public interface ICaseTypeMenuService
{
    /// <summary>
    /// Get case types in a menu format
    /// </summary>
    /// <returns>Result set of <see cref="CaseTypeMenu"/></returns>
    Task<ResultSet<CaseTypeMenu>> GetMenuItems(ListOptions options);

    /// <summary>Get a list of cases as defined by <see cref="GetCasesListFilter"/> and the role of the user.</summary>
    /// <param name="user">The user that initiated the request</param>
    /// <param name="options">The case list filters.</param>
    /// <param name="caseTypeId">The case type id.</param>
    /// <returns></returns>
    Task<ResultSet<CasePartial>> GetCasesByCaseTypeId(ClaimsPrincipal user, ListOptions<GetCasesListFilter> options, Guid caseTypeId);
}
