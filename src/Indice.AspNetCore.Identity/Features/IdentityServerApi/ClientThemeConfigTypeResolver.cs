using System;

namespace Indice.AspNetCore.Identity
{
    internal class ClientThemeConfigTypeResolver
    {
        private readonly Type _type;

        public ClientThemeConfigTypeResolver(Type resolver) {
            _type = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public Type ResolveType() => _type;
    }
}
