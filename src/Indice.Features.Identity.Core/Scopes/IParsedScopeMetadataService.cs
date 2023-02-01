using System.Globalization;
using System.Threading.Tasks;
using IdentityServer4.Validation;

namespace Indice.Features.Identity.Core.Scopes
{
    /// <summary>
    /// Metadata DTO class for retrieving additional information about a <see cref="ParsedScopeValue"/>
    /// </summary>
    public class ParsedScopeMetadata
    {
        /// <summary>
        /// The parsed scope value.
        /// </summary>
        public ParsedScopeValue Scope { get; set; }
        /// <summary>
        /// The display name.
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// The description enhanced with any metadata.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// A flag that indicates if this scope needs strong customer authentication.
        /// </summary>
        public bool RequiresSca { get; set; }
    }

    /// <summary>
    /// A service that contains methods for resolving additional information about a parsed scope.
    /// </summary>
    public interface IParsedScopeMetadataService
    {
        /// <summary>
        /// Gets additional information about a parsed scope.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="culture">The current culture.</param>
        Task<ParsedScopeMetadata> ResolveMetadata(ParsedScopeValue scope, CultureInfo culture = null);
    }

    /// <summary>
    /// Extension methods enhancing the <see cref="IParsedScopeMetadataService"/>.
    /// </summary>
    public static class ParsedScopeMetadataServiceExtensions
    {
        /// <inheritdoc />
        public static async Task<TMetadata> ResolveMetadata<TMetadata>(this IParsedScopeMetadataService metadataService, ParsedScopeValue parsedScope, CultureInfo culture = null) where TMetadata : ParsedScopeMetadata
            => (TMetadata)(await metadataService.ResolveMetadata(parsedScope, culture));
    }
}
