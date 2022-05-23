using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// extensions on the <see cref="RedirectContext{TOptions}"/>. This is found when working with external authentication identity providers
    /// throught oauth and openid connect.
    /// </summary>
    public static class RedirectContextExtensions
    {
        private const string LoginProviderKey = "LoginProvider";
        /// <summary>
        /// relay the prompt parameter found in the original redirect url to the downstream identity provider.
        /// </summary>
        /// <typeparam name="TOptions"></typeparam>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Task HandlePromptAndRedirect<TOptions>(this RedirectContext<TOptions> context) where TOptions : AuthenticationSchemeOptions {
            string promptKey = "prompt";
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
