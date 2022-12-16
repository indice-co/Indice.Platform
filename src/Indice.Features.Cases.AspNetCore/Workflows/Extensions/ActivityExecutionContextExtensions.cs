using System.Security.Claims;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Cases.Workflows.Extensions
{
    /// <summary>
    /// Extensions for <see cref="ActivityExecutionContext"/>.
    /// </summary>
    public static class ActivityExecutionContextExtensions
    {
        /// <summary>
        /// Try to get the user from the Workflow context variable "RunAsSystemUser" or from the HttpContext.
        /// </summary>
        /// <param name="context">The activity execution context.</param>
        /// <returns></returns>
        public static ClaimsPrincipal TryGetUser(this ActivityExecutionContext context) {
            var runAsSystemUser = context.GetVariable<bool>("RunAsSystemUser");
            return runAsSystemUser
                ? Cases.Extensions.PrincipalExtensions.SystemUser()
                : GetHttpContextUser(context);
        }

        /// <summary>
        /// Get the HttpContext User from the <see cref="IHttpContextAccessor"/> interface.
        /// </summary>
        /// <param name="context">The activity execution context.</param>
        /// <returns></returns>
        public static ClaimsPrincipal GetHttpContextUser(this ActivityExecutionContext context) {
            var httpContextAccessor = context.GetService<IHttpContextAccessor>();
            return httpContextAccessor.HttpContext?.User;
        }
    }
}