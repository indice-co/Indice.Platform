using System.Collections.Generic;
using System.Linq;
using IdentityModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.WebUtilities;

namespace Indice.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// A view location expander that overrides view search locations when a client_id query string parameter exists in the URL.
    /// Suppose we have an AccountController with a Login view. The default view location is Views/Account/Login. When a client with id <b>spa</b>
    /// exists in the query string, then a new view location takes precedence which is Views/Account/spa/Login.
    /// </summary>
    public class ClientAwareViewLocationExpander : IViewLocationExpander
    {
        private const string ValueKey = "client_id";
        private const string ReturnUrlQueryParameterName = "returnUrl";
        private HttpContext _httpContext;

        /// <inheritdoc />
        public void PopulateValues(ViewLocationExpanderContext context) {
            var actionContext = context.ActionContext;
            _httpContext = actionContext.HttpContext;
            var clientId = ExtractClientId();
            context.Values[ValueKey] = clientId ?? string.Empty;
        }

        /// <inheritdoc />
        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations) {
            context.Values.TryGetValue(ValueKey, out var value);
            if (string.IsNullOrEmpty(value)) {
                return viewLocations;
            }
            var locations = ExpandViewLocationsCore(viewLocations, value);
            return locations;
        }

        private IEnumerable<string> ExpandViewLocationsCore(IEnumerable<string> viewLocations, string value) {
            foreach (var location in viewLocations) {
                yield return location.Replace("{0}", value + "/{0}");
                yield return location;
            }
        }

        private string ExtractClientId() {
            var queryStringValues = _httpContext.Request.Query;
            var clientId = default(string);
            if (queryStringValues.ContainsKey(JwtClaimTypes.ClientId)) {
                clientId = queryStringValues[JwtClaimTypes.ClientId].ToString();
                return clientId;
            }
            var returnUrl = queryStringValues.ContainsKey(ReturnUrlQueryParameterName) ? queryStringValues[ReturnUrlQueryParameterName].ToString() : string.Empty;
            if (string.IsNullOrEmpty(returnUrl)) {
                return default;
            }
            clientId = QueryHelpers.ParseQuery(returnUrl).Where(x => x.Key.Contains(JwtClaimTypes.ClientId)).FirstOrDefault().Value;
            return clientId;
        }
    }
}
