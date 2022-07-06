using System.Security.Claims;
using System.Threading.Tasks;
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
        public Task<GetCasesListFilter> Filter(ClaimsPrincipal user, GetCasesListFilter filter);
        public Task<bool> IsValid(ClaimsPrincipal user, CaseDetails theCase);
    }
}
