using System.Security.Claims;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;

namespace Indice.Features.Cases.Core.Services;

internal class AggregateCaseAuthorizationProvider : ICaseAuthorizationProvider
{
    private readonly IEnumerable<ICaseAuthorizationService> _caseAuthorizationServices;

    public AggregateCaseAuthorizationProvider(IEnumerable<ICaseAuthorizationService> listOfServices) {
        _caseAuthorizationServices = listOfServices ?? throw new ArgumentNullException(nameof(listOfServices));
    }

    public async Task<IQueryable<CasePartial>> GetCaseMembership(IQueryable<CasePartial> cases, ClaimsPrincipal user) {
        foreach (var authorizationService in _caseAuthorizationServices) {
            cases = await authorizationService.GetCaseMembership(cases, user);
        }
        return cases;
    }

    public async Task<bool> IsMember(ClaimsPrincipal user, Case @case) {
        foreach (var authorizationService in _caseAuthorizationServices) {
            if (!await authorizationService.IsMember(user, @case)) {
                return false;
            }
        }
        return true;
    }
}
