using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;

namespace Indice.Features.Cases.Services
{
    internal class AggregateCaseAuthorizationProvider : ICaseAuthorizationProvider
    {
        private readonly IEnumerable<ICaseAuthorizationService> listOfServices;

        public AggregateCaseAuthorizationProvider(IEnumerable<ICaseAuthorizationService> listOfServices) {
            this.listOfServices = listOfServices ?? throw new ArgumentNullException(nameof(listOfServices));
        }
        public async Task<GetCasesListFilter> Filter(ClaimsPrincipal user, GetCasesListFilter filter) {
            foreach(var authzService in listOfServices) {
                filter = await authzService.ApplyFilterFor(user, filter);
            }
            return filter;
        }

        public async Task<bool> IsValid(ClaimsPrincipal user, CaseDetails theCase) {
            foreach (var authzService in listOfServices) {
                if(!await authzService.IsValid(user, theCase)) {
                    return false;
                }
            }
            return true;
        }
    }
}
