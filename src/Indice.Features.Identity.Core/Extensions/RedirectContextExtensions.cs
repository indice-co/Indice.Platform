using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>Extensions on the <see cref="RedirectContext{TOptions}"/>. This is found when working with external authentication identity providers through OAuth and OpenID connect.</summary>
    public static class RedirectContextExtensions
    {
        private const string LoginProviderKey = "LoginProvider";

        /// <summary>Relay the prompt parameter found in the original redirect URL to the downstream identity provider.</summary>
        /// <typeparam name="TOptions">Contains the options used by the <see cref="AuthenticationHandler{T}"/>.</typeparam>
        /// <param name="context">Context passed for redirect events.</param>
        public static Task HandlePromptAndRedirect<TOptions>(this RedirectContext<TOptions> context) where TOptions : AuthenticationSchemeOptions {
            var promptKey = "prompt";
            if (context.Properties.Items.TryGetValue(promptKey, out var prompt)) {
                context.Properties.Items.TryGetValue(LoginProviderKey, out var loginProvider);
                switch (loginProvider) {
                    case "Google":
                        if (prompt == "login") {
                            prompt = "consent";
                        }
                        break;
                    case "Facebook":
                        if (prompt == "login" || prompt == "consent") {
                            promptKey = "auth_type";
                            prompt = "reauthenticate";
                        }
                        break;
                    default:
                        break;
                }
                context.RedirectUri = QueryHelpers.AddQueryString(context.RedirectUri, promptKey, prompt);
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }
    }
}
