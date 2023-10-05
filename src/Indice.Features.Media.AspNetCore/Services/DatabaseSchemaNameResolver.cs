namespace Indice.Features.Media.AspNetCore.Services;
/// <summary>Resolver for Media API feature database schema name.</summary>
public class DatabaseSchemaNameResolver
{
    private readonly string _schemaName;

    /// <summary>Creates a new instance of <see cref="DatabaseSchemaNameResolver"/>.</summary>
    /// <param name="schemaName">The schema name.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DatabaseSchemaNameResolver(string schemaName) {
        _schemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    /// <summary>Gets the configured schema name.</summary>
    /// <returns></returns>
    public string GetSchemaName() => _schemaName;
}
