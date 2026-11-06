namespace Indice.EntityFrameworkCore;

/// <summary>The database engine (SQL dialect) used to translate the <see cref="Functions.JsonFunctions"/> to SQL.</summary>
public enum SqlDialect
{
    /// <summary>Microsoft SQL Server (default). Uses <c>JSON_VALUE</c> and <c>CONVERT</c>.</summary>
    SqlServer = 0,
    /// <summary>PostgreSQL. Uses <c>jsonb_extract_path_text</c> and <c>CAST</c>.</summary>
    PostgreSql = 1
}

/// <summary>Extension methods for the <see cref="SqlDialect"/>.</summary>
public static class SqlDialectExtensions
{
    /// <summary>The provider name of the Npgsql (PostgreSQL) Entity Framework Core provider.</summary>
    public const string NpgsqlProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";

    /// <summary>Resolves the <see cref="SqlDialect"/> from the Entity Framework Core provider name (usually <c>DbContext.Database.ProviderName</c>).</summary>
    /// <param name="providerName">The Entity Framework Core provider name.</param>
    /// <returns>The matching <see cref="SqlDialect"/>. Defaults to <see cref="SqlDialect.SqlServer"/>.</returns>
    public static SqlDialect GetSqlDialect(string? providerName) =>
        NpgsqlProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase) ? SqlDialect.PostgreSql : SqlDialect.SqlServer;
}
