using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Validation;

namespace Indice.AspNetCore.Identity.Scopes
{

    /// <summary>
    /// Dynamic scopes Decorator for <see cref="IIntrospectionResponseGenerator"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="IdentityServer4.ResponseHandling.IIntrospectionResponseGenerator" />
    public class DynamicScopeIntrospectionResponseGenerator<T> : IIntrospectionResponseGenerator where T : IIntrospectionResponseGenerator
    {
        private readonly IIntrospectionResponseGenerator _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntrospectionResponseGenerator" /> class.
        /// </summary>
        /// <param name="events">The events.</param>
        /// <param name="logger">The logger.</param>
        public DynamicScopeIntrospectionResponseGenerator(T inner) {
            _inner = inner;
        }

        /// <summary>
        /// Processes the response.
        /// </summary>
        /// <param name="validationResult">The validation result.</param>
        /// <returns></returns>
        public async Task<Dictionary<string, object>> ProcessAsync(IntrospectionRequestValidationResult validationResult) {
            var response = await _inner.ProcessAsync(validationResult);
            // calculate scopes the caller is allowed to see
            var scopeNames = validationResult.Claims.Where(c => c.Type == JwtClaimTypes.Scope).Select(x => x.Value);
            var scopes = validationResult.Api.Scopes.FindMatches(scopeNames).Select(x => x.Name);
            if (response.ContainsKey("scope")) {
                response["scope"] = string.Join(' ', scopes);
            } else {
                response.Add("scope", string.Join(' ', scopes));
            }
            return response;
        }

    }
}
