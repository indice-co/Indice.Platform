using System.Collections.Generic;
using IdentityModel;
using Microsoft.AspNetCore.WebUtilities;

namespace CodeFlowInlineFrame.Models
{
    public class AuthorizationResponse
    {
        public string Code { get; set; }
        public string IdToken { get; set; }
        public IEnumerable<string> Scopes { get; set; }
        public string SessionState { get; set; }

        public void PopulateFrom(string queryString) {
            var queryStringValues = QueryHelpers.ParseQuery(queryString);
            Code = queryStringValues.TryGetValue(OidcConstants.AuthorizeResponse.Code, out var code) ? code : default;
            IdToken = queryStringValues.TryGetValue(OidcConstants.AuthorizeResponse.IdentityToken, out var idToken) ? idToken : default;
            Scopes = queryStringValues.TryGetValue(OidcConstants.AuthorizeResponse.Scope, out var scope) ? ((string)scope).Split(' ') : default;
            SessionState = queryStringValues.TryGetValue("session_state", out var sessionState) ? sessionState : default;
        }
    }
}
