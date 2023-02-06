namespace Indice.Features.Identity.Core.Scopes
{
    /// <summary>Options for configuring <see cref="IParsedScopeNotificationService"/>.</summary>
    public class ParsedScopeNotificationOptions
    {
        /// <summary>The endpoint to use when notifying an API that a grant was removed or updated.</summary>
        public string Endpoint { get; set; }
    }
}
