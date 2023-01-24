using System.Security.Claims;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;

namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// This is the interface for the class
    /// that gets all authz services and 
    /// combines them.
    /// 
    /// it has to be of different type that 
    /// ICaseAuthorizationService so as not to 
    /// be a member of the list of ICaseAuthorizationServices
    /// </summary>
    internal interface ICaseAuthorizationProvider
    {
        /// <summary>
        /// Apply filtering against a user according to his <see cref="ClaimsPrincipal"/> and .
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="filter">The filter to apply.</param>
        /// <returns></returns>
        public Task<GetCasesListFilter> Filter(ClaimsPrincipal user, GetCasesListFilter filter);

        /// <summary>
        /// Validates that a user is authorized against a list of <see cref="ICaseAuthorizationService"/> for a <see cref="Case"/>.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="case">The case.</param>
        /// <returns></returns>
        public Task<bool> IsValid(ClaimsPrincipal user, Case @case);
    }
}
