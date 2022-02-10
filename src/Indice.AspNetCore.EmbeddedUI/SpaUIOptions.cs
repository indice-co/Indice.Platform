using System;
using Microsoft.AspNetCore.StaticFiles;

namespace Indice.AspNetCore.EmbeddedUI
{

    /// <summary>
    /// Options for configuring <see cref="SpaUIMiddleware"/> middleware.
    /// </summary>
    public class SpaUIOptions
    {
        internal string Version { get; set; }
        private string _defaultPath = "/backoffice";
        /// <summary>
        /// The name of the section used in appsettings.json file.
        /// </summary>
        public const string SectionName = "SpaUI";
        /// <summary>
        /// The base address of the authority server (i.e. https://identity.example.com).
        /// </summary>
        public string Authority { get; set; }
        /// <summary>
        /// The client id used to identify the application in the authority server. Defaults to <b>spa-ui</b>.
        /// </summary>
        public string ClientId { get; set; } = "spa-ui";
        /// <summary>
        /// The default scope names, as space separated values, to be used for Campaigns API. This can include the main scope (audience) and sub-scopes. Defaults to <b>campaigns</b>.
        /// </summary>
        /// <example>backoffice backoffice:campaigns</example>
        public string Scope { get; set; } = "campaigns";
        /// <summary>
        /// Specifies the title (shown in browser tab) used in the back-office application. Defaults to <b>Indice BackOffice UI</b>.
        /// </summary>
        public string DocumentTitle { get; set; } = "Indice BackOffice UI";
        /// <summary>
        /// Gets or sets additional content to place in the head of the SPA page.
        /// </summary>
        public string HeadContent { get; set; }
        /// <summary>
        /// Specifies whether the back-office application is served in the specified path, as dictated by the <see cref="Path"/> property.
        /// Can be used in scenarios where the back-office application needs to be hidden. Defaults to true.
        /// </summary>
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// The base address of the application host (i.e. https://example.com).
        /// </summary>
        public string Host { get; set; }
        /// <summary>
        /// URI to redirect to after logout.
        /// </summary>
        public string PostLogoutRedirectUri { get; set; }
        /// <summary>
        /// The path that the back-office application is served. Defaults to '/backoffice'.
        /// </summary>
        /// <example>https://identity.example.com/backoffice</example>
        public string Path {
            get => _defaultPath;
            set => _defaultPath = string.IsNullOrWhiteSpace(value) ? _defaultPath : $"/{value.Trim('/')}";
        }
        /// <summary>
        /// Called after the status code and headers have been set, but before the body has been written. This can be used to add or change the response headers.
        /// </summary>
        public Action<StaticFileResponseContext> OnPrepareResponse { get; set; }
    }
}
