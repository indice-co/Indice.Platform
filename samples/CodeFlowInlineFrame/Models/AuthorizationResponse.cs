using System.Collections.Generic;
using IdentityModel;
using Microsoft.AspNetCore.Http;

namespace CodeFlowInlineFrame.Models
{
    public class AuthorizationResponse
    {
        public string Code { get; private set; }
        public string IdToken { get; private set; }
        public IEnumerable<string> Scopes { get; private set; }
        public string SessionState { get; private set; }
        public string State { get; private set; }

        public void PopulateFrom(IFormCollection form) {
            Code = form.TryGetValue(OidcConstants.AuthorizeResponse.Code, out var code) ? code : default;
            IdToken = form.TryGetValue(OidcConstants.AuthorizeResponse.IdentityToken, out var idToken) ? idToken : default;
            Scopes = form.TryGetValue(OidcConstants.AuthorizeResponse.Scope, out var scope) ? ((string)scope).Split(' ') : default;
            SessionState = form.TryGetValue("session_state", out var sessionState) ? sessionState : default;
            State = form.TryGetValue(OidcConstants.AuthorizeResponse.State, out var state) ? state : default;
        }
    }
}
