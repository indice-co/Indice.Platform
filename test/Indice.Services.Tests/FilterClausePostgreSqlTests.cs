using Indice.EntityFrameworkCore;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Services.Tests;

public class FilterClausePostgreSqlTranslationTests
{
    private static DummyDbContext CreateDbContext(Action<DbContextOptionsBuilder> configure) {
        var builder = new DbContextOptionsBuilder<DummyDbContext>();
        configure(builder);
        var dbContext = new DummyDbContext(builder.Options);
        // Build the model so that the json columns are registered before building the dynamic expressions.
        _ = dbContext.Model;
        return dbContext;
    }

    [Fact]
    public void SqlDialect_IsResolved_FromProviderName() {
        Assert.Equal(SqlDialect.PostgreSql, SqlDialectExtensions.GetSqlDialect("Npgsql.EntityFrameworkCore.PostgreSQL"));
        Assert.Equal(SqlDialect.SqlServer, SqlDialectExtensions.GetSqlDialect("Microsoft.EntityFrameworkCore.SqlServer"));
        Assert.Equal(SqlDialect.SqlServer, SqlDialectExtensions.GetSqlDialect(null));
    }

    [Fact]
    public void FilterClause_Translates_ToPostgreSqlQuery_Test() {
        using var dbContext = CreateDbContext(builder => builder.UseNpgsql("Host=localhost;Database=Indice.FilterClause.Test"));
        var filters = new List<FilterClause> {
            (FilterClause)"data.displayName::contains::κων",
            (FilterClause)"data.period.to::gt::(DateTime)2024-01-17",
            (FilterClause)"metadata.NAME::eq::Thanos",
        };
        var sql = dbContext.Dummies.Where(filters).ToQueryString();
        Assert.DoesNotContain("JSON_VALUE", sql);
        Assert.Contains("jsonb_extract_path_text(d.\"Data\"::jsonb, 'displayName')", sql);
        Assert.Contains("jsonb_extract_path_text(d.\"Data\"::jsonb, 'period', 'to')::timestamp ", sql);
        Assert.Contains("jsonb_extract_path_text(d.\"Metadata\"::jsonb, 'NAME')", sql);
    }

    [Fact]
    public void Sort_Translates_ToPostgreSqlQuery_Test() {
        using var dbContext = CreateDbContext(builder => builder.UseNpgsql("Host=localhost;Database=Indice.FilterClause.Test"));
        var query = dbContext.Dummies.AsQueryable();
        foreach (var sorting in new ListOptions { Sort = "(datetime)data.period.from+,(number)data.Balance-,data.displayName" }.GetSortings()) {
            query = query.OrderBy(sorting, append: true);
        }
        var sql = query.ToQueryString();
        Assert.DoesNotContain("JSON_VALUE", sql);
        Assert.Contains("jsonb_extract_path_text(d.\"Data\"::jsonb, 'period', 'from')::timestamp,", sql);
        Assert.Contains("jsonb_extract_path_text(d.\"Data\"::jsonb, 'Balance')::double precision DESC", sql);
        Assert.Contains("jsonb_extract_path_text(d.\"Data\"::jsonb, 'displayName')", sql);
    }

    [Fact]
    public void FilterClause_Translates_ToSqlServerQuery_Test() {
        using var dbContext = CreateDbContext(builder => builder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=Indice.FilterClause.Test"));
        var filters = new List<FilterClause> {
            (FilterClause)"data.displayName::contains::κων",
            (FilterClause)"data.period.to::gt::(DateTime)2024-01-17",
        };
        var sql = dbContext.Dummies.Where(filters).ToQueryString();
        Assert.Contains("JSON_VALUE([d].[Data], N'$.displayName')", sql);
        Assert.Contains("Convert(datetime2, JSON_VALUE([d].[Data], N'$.period.to'))", sql);
        Assert.DoesNotContain("jsonb_extract_path_text", sql);
    }
}

/// <summary>Runs the queries against a live PostgreSQL server. Set the <c>INDICE_TESTS_POSTGRESQL</c> environment variable to a connection string of a server where databases can be created to enable them.</summary>
public class FilterClausePostgreSqlQueryableExtensionTests : IAsyncLifetime
{
    private const string ConnectionStringVariable = "INDICE_TESTS_POSTGRESQL";
    private readonly string? _connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
    private ServiceProvider? _serviceProvider;

    private DummyDbContext DbContext => _serviceProvider!.GetRequiredService<DummyDbContext>();

    [Fact]
    public async Task FilterClause_Executes_OnPostgreSql_Test() {
        Assert.SkipWhen(_serviceProvider is null, $"Set the {ConnectionStringVariable} environment variable to run against PostgreSQL.");
        var filters = new List<FilterClause> {
            (FilterClause)"data.displayName::contains::Κων",
            (FilterClause)$"data.period.to::gt::(DateTime){DateTime.Now:yyyy-MM-dd}",
            (FilterClause)"metadata.NAME::eq::Thanos",
        };
        var results = await DbContext.Dummies.Where(filters).ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(results);
        results = await DbContext.Dummies.Where([(FilterClause)"data.balance::gt::(number)300", (FilterClause)"data.order::lt::(integer)0"]).ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(results);
        results = await DbContext.Dummies.Where([(FilterClause)"data.birthDate::lt::(DateTime)1980-01-01T00:00:00Z"]).ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(results);
    }

    [Fact]
    public async Task ToResultset_Translates_DynamicJsonPaths_Sort_OnPostgreSql_Test() {
        Assert.SkipWhen(_serviceProvider is null, $"Set the {ConnectionStringVariable} environment variable to run against PostgreSQL.");
        var query = DbContext.Dummies;
        foreach (var sort in new[] { "data.displayName,name", "(integer)data.order", "(datetime)data.period.from+", "(datetime)data.birthDate-", "(number)data.Balance-", "(boolean)data.enabled-" }) {
            var results = await query.ToResultSetAsync(new ListOptions { Sort = sort }, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(3, results.Count);
        }
        var ordered = await query.ToResultSetAsync(new ListOptions { Sort = "(number)data.balance-" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Γιάννης", ordered.Items[0].Name);
    }

    public async ValueTask InitializeAsync() {
        if (string.IsNullOrWhiteSpace(_connectionString)) {
            return;
        }
        var connectionString = $"{_connectionString.TrimEnd(';')};Database=Indice.FilterClause.Test_{Environment.Version.Major}_{Guid.NewGuid():N}";
        _serviceProvider = new ServiceCollection()
            .AddDbContext<DummyDbContext>(builder => builder.UseNpgsql(connectionString), ServiceLifetime.Singleton)
            .BuildServiceProvider();
        await DbContext.SeedAsync();
    }

    public async ValueTask DisposeAsync() {
        if (_serviceProvider is null) {
            return;
        }
        await DbContext.Database.EnsureDeletedAsync();
        await _serviceProvider.DisposeAsync();
    }
}
