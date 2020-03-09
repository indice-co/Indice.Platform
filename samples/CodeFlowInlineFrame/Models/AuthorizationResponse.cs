using System.Collections.Generic;
using IdentityModel;
using Microsoft.AspNetCore.WebUtilities;

namespace CodeFlowInlineFrame.Models
{
    public class AuthorizationResponse
    {
        public string Code { get; private set; }
        public string IdToken { get; private set; }
        public IEnumerable<string> Scopes { get; private set; }
        public string SessionState { get; private set; }
        public string State { get; private set; }

        public void PopulateFrom(string queryString) {
            var queryStringValues = QueryHelpers.ParseQuery(queryString);
            Code = queryStringValues.TryGetValue(OidcConstants.AuthorizeResponse.Code, out var code) ? code : default;
            IdToken = queryStringValues.TryGetValue(OidcConstants.AuthorizeResponse.IdentityToken, out var idToken) ? idToken : default;
            Scopes = queryStringValues.TryGetValue(OidcConstants.AuthorizeResponse.Scope, out var scope) ? ((string)scope).Split(' ') : default;
            SessionState = queryStringValues.TryGetValue("session_state", out var sessionState) ? sessionState : default;
            State = queryStringValues.TryGetValue(OidcConstants.AuthorizeResponse.State, out var state) ? state : default;
        }
    }
}
