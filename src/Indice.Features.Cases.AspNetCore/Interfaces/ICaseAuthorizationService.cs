using System.Security.Claims;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;

namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// When a case list is requested by the BO, it is filtered based on rules
    /// that are set in services implementing this interface
    /// </summary>
    public interface ICaseAuthorizationService
    {
        /// <summary>
        /// When a IAdminCaseService is asked for a list,
        /// the filter is first passed through here
        /// </summary>
        /// <param name="user"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public Task<GetCasesListFilter> ApplyFilterFor(ClaimsPrincipal user, GetCasesListFilter filter);
        
        /// <summary>
        /// When a caseId is requested, it must return true in order to reach the 
        /// caller
        /// </summary>
        /// <param name="user"></param>
        /// <param name="caseDetails"></param>
        /// <returns></returns>
        public Task<bool> IsValid(ClaimsPrincipal user, CaseDetails caseDetails);
    }
}