using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Validator for <see cref="CreateClaimTypeRequest"/> model.
    /// </summary>
    internal class CreateClaimTypeRequestValidationFilter : IAsyncActionFilter
    {
        private readonly Func<ExtendedIdentityDbContext<User, Role>> _getDbContext;

        /// <summary>
        /// Creates a new instance of <see cref="CreateClaimTypeRequestValidationFilter"/>.
        /// </summary>
        /// <param name="getDbContext"></param>
        public CreateClaimTypeRequestValidationFilter(Func<ExtendedIdentityDbContext<User, Role>> getDbContext) {
            _getDbContext = getDbContext ?? throw new ArgumentNullException(nameof(getDbContext));
        }

        /// <summary>
        /// A filter that asynchronously surrounds execution of the action, after model binding is complete.
        /// </summary>
        /// <param name="context">The <see cref="ActionExecutingContext"/>.</param>
        /// <param name="next">The <see cref="ActionExecutionDelegate"/>. Invoked to execute the next action filter or the action itself.</param>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
            var body = context.ActionArguments.SingleOrDefault(x => x.Value is CreateClaimTypeRequest);
            if (body.Value == null) {
                context.Result = new BadRequestObjectResult(new ValidationProblemDetails(new Dictionary<string, string[]> {
                    { nameof(CreateClaimTypeRequest.Name).Camelize(), new string[] { $"Request body cannot be null." } }
                }));
                return;
            }
            var claimTypeName = ((CreateClaimTypeRequest)body.Value).Name;
            var dbContext = _getDbContext();
            var exists = await dbContext.ClaimTypes.AsNoTracking().AnyAsync(x => x.Name == claimTypeName);
            if (exists) {
                context.Result = new BadRequestObjectResult(new ValidationProblemDetails(new Dictionary<string, string[]> {
                    { nameof(CreateClaimTypeRequest.Name).Camelize(), new string[] { $"A claim type with name {claimTypeName} already exists." } }
                }));
                return;
            }
            await next();
        }
    }
}
