namespace Indice.Features.Identity.Core.Scopes
{
    /// <summary>Options for configuring <see cref="IParsedScopeMetadataService"/>.</summary>
    public class ParsedScopeMetadataOptions
    {
        /// <summary>The endpoint to use when notifying an API that a grant was removed or updated.</summary>
        public string Endpoint { get; set; }
    }
}
