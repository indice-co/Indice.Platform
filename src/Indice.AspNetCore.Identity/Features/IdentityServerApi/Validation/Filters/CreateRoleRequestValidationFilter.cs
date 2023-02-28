using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Indice.AspNetCore.Identity.Api.Models;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Api.Filters;

/// <summary>Validator for <see cref="CreateRoleRequest"/> model.</summary>
internal class CreateRoleRequestValidationFilter : IAsyncActionFilter
{
    private readonly ExtendedIdentityDbContext<DbUser, DbRole> _dbContext;

    /// <summary>Creates a new instance of <see cref="CreateClaimTypeRequestValidationFilter"/>.</summary>
    /// <param name="dbContext">An extended <see cref="DbContext"/> for the Identity framework.</param>
    public CreateRoleRequestValidationFilter(ExtendedIdentityDbContext<DbUser, DbRole> dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>A filter that asynchronously surrounds execution of the action, after model binding is complete.</summary>
    /// <param name="context">The <see cref="ActionExecutingContext"/>.</param>
    /// <param name="next">The <see cref="ActionExecutionDelegate"/>. Invoked to execute the next action filter or the action itself.</param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        var body = context.ActionArguments.SingleOrDefault(x => x.Value is CreateRoleRequest);
        if (body.Value == null) {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(new Dictionary<string, string[]> {
                { nameof(CreateClaimTypeRequest.Name).Camelize(), new string[] { $"Request body cannot be null." } }
            }));
            return;
        }
        var roleName = ((CreateRoleRequest)body.Value).Name;
        var exists = await _dbContext.Roles.AsNoTracking().AnyAsync(x => x.Name == roleName);
        if (exists) {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(new Dictionary<string, string[]> {
                { nameof(CreateClaimTypeRequest.Name).Camelize(), new string[] { $"A role with name {roleName} already exists." } }
            }));
            return;
        }
        await next();
    }
}
