using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity
{
    /// <summary></summary>
    public static class IdentityBuilderUIExtensions
    {
        /// <summary></summary>
        /// <param name="builder"></param>
        public static IdentityBuilder AddDefaultIdentityUI(this IdentityBuilder builder) {
            return builder;
        }
    }
}
