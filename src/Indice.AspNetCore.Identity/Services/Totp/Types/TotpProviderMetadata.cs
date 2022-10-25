using Indice.Services;

namespace Indice.AspNetCore.Identity
{
    /// <summary>TOTP provider metadata.</summary>
    public class TotpProviderMetadata
    {
        /// <summary>The provider type.</summary>
        public TotpProviderType Type { get; set; }
        /// <summary>The provider channel.</summary>
        public TotpDeliveryChannel Channel { get; set; }
        /// <summary>The name which is used to register the provider in the list of token providers.</summary>
        public string Name => $"{Channel}";
        /// <summary>The display name.</summary>
        public string DisplayName { get; set; }
        /// <summary>Can generate TOTP. If false this provider can only validate TOTPs.</summary>
        public bool CanGenerate { get; set; }
    }
}
