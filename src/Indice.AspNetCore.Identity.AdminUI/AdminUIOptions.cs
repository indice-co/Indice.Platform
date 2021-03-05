using System;
using Microsoft.AspNetCore.StaticFiles;

namespace Indice.AspNetCore.Identity.AdminUI
{
    /// <summary>
    /// Options for configuring <see cref="AdminUIMiddleware"/> middleware.
    /// </summary>
    public class AdminUIOptions
    {
        private string _defaultPath = "/backoffice";
        /// <summary>
        /// The name of the section used in appsettings.json file.
        /// </summary>
        public const string SectionName = "AdminUI";
        /// <summary>
        /// The base address of the Identity Server instance (i.e. https://identity.example.com).
        /// </summary>
        public string Authority { get; set; }
        /// <summary>
        /// The client id used to identify the application in Identity Server. Defaults to 'idsrv-admin-ui';
        /// </summary>
        public string ClientId { get; set; } = "idsrv-admin-ui";
        /// <summary>
        /// Specifies the title (shown in browser tab) used in the back-office application. Defaults to 'Indice Admin UI'.
        /// </summary>
        public string DocumentTitle { get; set; } = "Indice Admin UI";
        /// <summary>
        /// Gets or sets additional content to place in the head of the SPA page
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
