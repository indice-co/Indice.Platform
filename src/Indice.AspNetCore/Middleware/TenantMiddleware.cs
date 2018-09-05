using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Indice.AspNetCore.Middleware
{
    /// <summary>
    /// Used to populate active tenant information to the current User <see cref="ClaimsPrincipal"/> in a multitenant application API.
    /// </summary>
    /// <typeparam name="TDbContext">MyDbContext that contains the <typeparamref name="TTenant"/> table</typeparam>
    /// <typeparam name="TTenant">The entity that represents the subscription</typeparam>
    public class TenantMiddleware<TDbContext, TTenant> 
        where TDbContext : DbContext 
        where TTenant : class, ITenant
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Route base is the segment of the route that all tenant operations are children. 
        /// Examples of RouteBase for a multitenant api
        /// </summary>
        protected string RouteBase { get; set; }

        /// <summary>
        /// Constructs a Subscription middleware passing the next request delegate in the pipeline.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="routeBase">
        /// Route base is the segment of the route that all tenant operations are children. 
        /// Examples of RouteBase for a multitenant api (subscriptions ... accounts ...)
        /// </param>
        public TenantMiddleware(RequestDelegate next, string routeBase) {
            _next = next;
            RouteBase = routeBase ?? "subscriptions";
        }

        /// <summary>
        /// Invokes the middleware
        /// </summary>
        /// <param name="context"></param>
        /// <param name="dbContext"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context, TDbContext dbContext, IDistributedCache cache) {
            if (dbContext == null) {
                throw new ArgumentNullException(nameof(dbContext));
            }

            if (cache == null) {
                throw new ArgumentNullException(nameof(cache));
            }

            var user = context.User;

            if (user != null && user.Identity.IsAuthenticated) {
                // Get the subscription id or company alias from route.
                var tenantId = GetTenantIdFromRoute(context.Request.Path, RouteBase ?? "subscriptions");

                if (!string.IsNullOrEmpty(tenantId)) {
                    var isGuid = Guid.TryParse(tenantId, out var guid);

                    // Check if the value in route is not a valid unique identifier. This means that the user provided us with the company alias.
                    if (!isGuid && typeof(ITenantWithAlias).IsAssignableFrom(typeof(TTenant))) {
                        // Get the subscription id by using the alias.
                        var id = await GetTenantIdFromAliasAsync(dbContext, tenantId);

                        if (id.HasValue) {
                            ((ClaimsIdentity)user.Identity).AddClaim(new Claim(BasicClaimTypes.TenantAlias, tenantId));
                            ((ClaimsIdentity)user.Identity).AddClaim(new Claim(BasicClaimTypes.TenantId, id.Value.ToString()));
                        }
                    } else {
                        ((ClaimsIdentity)user.Identity).AddClaim(new Claim(BasicClaimTypes.TenantId, tenantId));
                    }
                }
            }

            await _next(context);
        }

        /// <summary>
        /// Gets the tenantId segment from the current route. 
        /// </summary>
        /// <param name="route"></param>
        /// <param name="routeBase"></param>
        /// <returns></returns>
        public static string GetTenantIdFromRoute(string route, string routeBase) {
            var subscription = string.Empty;
            var index = route.IndexOf(routeBase, StringComparison.OrdinalIgnoreCase);

            if (index >= 0) {
                route = route.TrimEnd('/');
                var lastSegment = index + routeBase.Length == route.Length;

                if (!lastSegment) {
                    var segment = route.Substring(index + routeBase.Length + 1);
                    var slashIndex = segment.IndexOf('/');

                    if (slashIndex < 0) {
                        slashIndex = segment.IndexOf('?');
                    }

                    subscription = slashIndex >= 0 ? segment.Substring(0, slashIndex) : segment;
                }
            }

            return subscription;
        }

        /// <summary>
        /// Finds the id of a subscription by using the company alias.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="alias">The company alias.</param>
        /// <returns>Returns the subscription id as a string, if the alias exists, otherwise an empty string.</returns>
        public async Task<Guid?> GetTenantIdFromAliasAsync(TDbContext dbContext, string alias) {
            if (string.IsNullOrEmpty(alias)) {
                throw new ArgumentNullException(nameof(alias));
            }

            var subscriptionId = await dbContext.Set<TTenant>()
                                                .Where(s => ((ITenantWithAlias)s).Alias == alias)
                                                .Select(s => s.Id)
                                                .SingleOrDefaultAsync();

            return subscriptionId.Equals(Guid.Empty) ? default(Guid?) : subscriptionId;
        }
    }

    /// <summary>
    /// Extensions for the <see cref="TenantMiddleware{TDbContext, TTenant}"/>
    /// </summary>
    public static class TenantMiddlewareExtensions
    {
        /// <summary>
        ///  Used to populate active tenant information to the current User <see cref="ClaimsPrincipal"/> in a multitenant application API.
        /// </summary>
        /// <typeparam name="TDbContext">MyDbContext that contains the <typeparamref name="TTenant"/> table</typeparam>
        /// <typeparam name="TTenant">The entity that represents the subscription</typeparam>
        /// <param name="applicationBuilder"></param>
        /// <param name="routeBase">
        /// Route base is the segment of the route that all tenant operations are children. 
        /// Examples of RouteBase for a multitenant api (subscriptions ... accounts ...)
        /// </param>
        /// <returns></returns>
        public static IApplicationBuilder UseTenantFromRoute<TDbContext, TTenant>(this IApplicationBuilder applicationBuilder, string routeBase)
            where TDbContext : DbContext
            where TTenant : class, ITenant {
            applicationBuilder.UseMiddleware<TenantMiddleware<TDbContext, TTenant>>(routeBase);

            return applicationBuilder;
        }
    }
}
