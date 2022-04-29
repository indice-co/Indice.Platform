using System.Security.Claims;
using System.Threading.Tasks;
using Indice.Security;
using Microsoft.AspNetCore.Authentication;

namespace Microsoft.Extensions.DependencyInjection
{
    //https://leastprivilege.com/2020/07/06/flexible-access-token-validation-in-asp-net-core/
    //https://github.com/IdentityModel/IdentityModel.AspNetCore.AccessTokenValidation/blob/main/src/ScopeClaimsTransformer.cs
    /// <summary>
    /// The scope claim is a space delimited string. 
    /// This does not play nicely with .NET claims, hence we included a little claim transformer, that turns that string into individual claims:
    /// </summary>
    internal class ClaimsTransformation : IClaimsTransformation
    {
        /// <inheritdoc/>
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) => Task.FromResult(principal.NormalizeScopeClaims());
    }

    /// <summary>
    /// Contains the Logic for normalizing claims types coming from external identity providers to the JWT standard ones.    
    /// </summary>
    internal class ExternalIdentityProviderClaimsTransformation : IClaimsTransformation
    {
        private readonly string[] _claimTypesToIgnore;

        public ExternalIdentityProviderClaimsTransformation(params string[] claimTypesToIgnore) {
            _claimTypesToIgnore = claimTypesToIgnore;
        }

        /// <inheritdoc/>
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) => Task.FromResult(principal.NormalizeExternalProviderClaims(_claimTypesToIgnore));
    }

    /// <summary>
    /// Extensions on the ASP.NET Core authentication regarding <see cref="ClaimsTransformation"/>.
    /// </summary>
    public static class ClaimTransformationConfigurationExtensions
    {
        /// <summary>
        /// The scope claim is a space delimited string. 
        /// This does not play nicely with .NET claims, hence we included a little claim transformer, that turns that string into individual claims.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static IServiceCollection AddScopeTransformation(this IServiceCollection services) =>
            services.AddSingleton<IClaimsTransformation, ClaimsTransformation>();

        /// <summary>
        /// Adds a claim transformation that contains the Logic for normalizing claims types coming from external identity providers to the JWT standard ones.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="claimTypesToIgnore">Types to ignore</param>
        public static IServiceCollection AddExternalProviderClaimsTransformation(this IServiceCollection services, params string[] claimTypesToIgnore) =>
            services.AddSingleton<IClaimsTransformation>(serviceProvider => new ExternalIdentityProviderClaimsTransformation(claimTypesToIgnore));
    }
}
