using System.Security.Claims;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;

namespace Indice.Features.Cases.Services;

internal class AggregateCaseAuthorizationProvider : ICaseAuthorizationProvider
{
    private readonly IEnumerable<ICaseAuthorizationService> _caseAuthorizationServices;

    public AggregateCaseAuthorizationProvider(IEnumerable<ICaseAuthorizationService> listOfServices) {
        _caseAuthorizationServices = listOfServices ?? throw new ArgumentNullException(nameof(listOfServices));
    }

    public async Task<GetCasesListFilter> Filter(ClaimsPrincipal user, GetCasesListFilter filter) {
        foreach (var authorizationService in _caseAuthorizationServices) {
            filter = await authorizationService.ApplyFilterFor(user, filter);
        }
        return filter;
    }

    public async Task<bool> IsValid(ClaimsPrincipal user, Case @case) {
        foreach (var authorizationService in _caseAuthorizationServices) {
            if (!await authorizationService.IsValid(user, @case)) {
                return false;
            }
        }
        return true;
    }
}
