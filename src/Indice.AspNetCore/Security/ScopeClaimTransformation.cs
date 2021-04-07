using System.Security.Claims;
using System.Threading.Tasks;
using Indice.Security;
using Microsoft.AspNetCore.Authentication;

namespace Microsoft.Extensions.DependencyInjection
{
    //https://leastprivilege.com/2020/07/06/flexible-access-token-validation-in-asp-net-core/
    /// <summary>
    /// The scope claim is a space delimited string. 
    /// This does not play nicely with .NET claims, hence we included a little claim transformer, that turns that string into individual claims:
    /// </summary>
    internal class ScopeClaimTransformation : IClaimsTransformation
    {
        /// <inheritdoc/>
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) => Task.FromResult(principal.NormalizeScopeClaims());
    }

    /// <summary>
    /// Extensions on the aspnet core authentication regarding <see cref="ScopeClaimTransformation"/>.
    /// </summary>
    public static class ScopeClaimTransformationConfiguration
    {
        /// <summary>
        /// The scope claim is a space delimited string. 
        /// This does not play nicely with .NET claims, hence we included a little claim transformer, that turns that string into individual claims.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static IServiceCollection AddScopeTransformation(this IServiceCollection services) {
            return services.AddTransient<IClaimsTransformation, ScopeClaimTransformation>();
        }
    }
}
