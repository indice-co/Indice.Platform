namespace Indice.AspNetCore.Identity.Scopes
{
    /// <summary>
    /// Options for configuring <see cref="IDynamicScopeNotificationService"/>.
    /// </summary>
    public class DynamicScopeNotificationOptions
    {
        /// <summary>
        /// The endpoint to use when notifying an API that a grant was removed or updated.
        /// </summary>
        public string Endpoint { get; set; }
    }
}
