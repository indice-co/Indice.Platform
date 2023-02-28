namespace Indice.Features.Identity.Core;

/// <summary>Type resolver for client theme.</summary>
public class ClientThemeConfigTypeResolver
{
    private readonly Type _type;

    /// <summary>Creates a new instance of <see cref="ClientThemeConfigTypeResolver"/>.</summary>
    /// <param name="type"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ClientThemeConfigTypeResolver(Type type) {
        _type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>Resolves the type.</summary>
    public Type Resolve() => _type;
}
