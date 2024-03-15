using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Cases.Server.Options;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary>Manage lookups for the case types.</summary>
public static class LookupApi
{
        /// <summary>Maps lookup endpoint.</summary>
        public static IEndpointRouteBuilder MapLookup(this IEndpointRouteBuilder routes) {
            var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerEndpointOptions>>().Value;
            var group = routes.MapGroup($"{options.ApiPrefix}/manage/lookups");
            group.WithTags("Lookup");
            group.ProducesProblem(StatusCodes.Status500InternalServerError)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                    .ProducesProblem(StatusCodes.Status403Forbidden)
                    .ProducesProblem(StatusCodes.Status400BadRequest);
            group.WithGroupName(options.GroupName);
            var allowedScopes = new[] { options.ApiScope }.Where(x => x != null).Cast<string>().ToArray();
            group.RequireAuthorization(policy => policy
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(CasesApiConstants.AuthenticationScheme)
                .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
            ).RequireAuthorization(CasesApiConstants.Policies.BeCasesManager);
            group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
            group.MapGet("{lookupName}", LookupHandler.GetLookup)
                        .WithName(nameof(LookupHandler.GetLookup))
                        .WithSummary("Get a lookup result by lookupName and options.");
            return group;
        }
}
