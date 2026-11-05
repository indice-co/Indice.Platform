using Indice.EntityFrameworkCore;
using Indice.EntityFrameworkCore.Functions;
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
        Assert.Contains("jsonb_path_query_first(d.\"Data\"::jsonb, 'strict $.\"displayName\"'", sql);
        Assert.Contains("jsonb_path_query_first(d.\"Data\"::jsonb, 'strict $.\"period\".\"to\"'", sql);
        Assert.Contains("'\\1Z')::timestamptz > @", sql);
        Assert.Contains("(DbType = DateTime)", sql);
        Assert.Contains("jsonb_path_query_first(d.\"Metadata\"::jsonb, 'strict $.\"NAME\"'", sql);
        Assert.Contains("@.type() != \"object\" && @.type() != \"array\"", sql);
    }

    [Fact]
    public void CastToDate_UsesUtcTimestampLiterals_OnPostgreSql_Test() {
        using var dbContext = CreateDbContext(builder => builder.UseNpgsql("Host=localhost;Database=Indice.FilterClause.Test"));
        var date = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc);
        var sql = dbContext.Dummies.Where(x => JsonFunctions.CastToDate(x.Name!) >= EF.Constant(date)).ToQueryString();
        Assert.Contains("TIMESTAMPTZ '2024-01-17 00:00:00.0000000+00:00'", sql);
    }

    [Fact]
    public void JsonValue_Translates_RootPath_ToPostgreSqlQuery_Test() {
        using var dbContext = CreateDbContext(builder => builder.UseNpgsql("Host=localhost;Database=Indice.FilterClause.Test"));
        var sql = dbContext.Dummies.Where(x => JsonFunctions.JsonValue(x.Name!, "$") == "Thanos").ToQueryString();
        Assert.Contains("jsonb_path_query_first(d.\"Name\"::jsonb, 'strict $ ? (@.type() != \"object\" && @.type() != \"array\")')", sql);
    }

    [Theory]
    [InlineData("$.\"\\u03ba\"", "strict $.\"\\u03BA\"")]
    [InlineData("$[\"\\u03BA\\u03C9\"]", "strict $.\"\\u03BA\\u03C9\"")]
    [InlineData("$.\"a\\nb\"", "strict $.\"a\\nb\"")]
    [InlineData("$.\"a\\tb\\r\\b\\f\"", "strict $.\"a\\tb\\r\\b\\f\"")]
    [InlineData("$.\"a\\\"b\\\\c\\/d\"", "strict $.\"a\\u0022b\\\\c/d\"")]
    [InlineData("$['it\\'s']", "strict $.\"it\\u0027s\"")]
    [InlineData("$.data.items[0].\"some key\"", "strict $.\"data\".\"items\"[0].\"some key\"")]
    [InlineData("$.items[0]", "strict $.\"items\"[0]")]
    [InlineData("$.items.\"0\"", "strict $.\"items\".\"0\"")]
    public void JsonPath_DecodesEscapes_AndPreservesSegmentKinds_Test(string path, string expected) {
        using var dbContext = CreateDbContext(builder => builder.UseNpgsql("Host=localhost;Database=Indice.FilterClause.Test"));
        var sql = dbContext.Dummies.Where(x => JsonFunctions.JsonValue(x.Data!, path) == "Thanos").ToQueryString();
        Assert.Contains($"'{expected.Replace("'", "''")}'", sql);
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
        Assert.Contains("jsonb_path_query_first(d.\"Data\"::jsonb, 'strict $.\"period\".\"from\"'", sql);
        Assert.Contains("'\\1Z')::timestamptz,", sql);
        Assert.Contains("'0')::double precision DESC", sql);
        Assert.Contains("jsonb_path_query_first(d.\"Data\"::jsonb, 'strict $.\"Balance\"'", sql);
        Assert.Contains("jsonb_path_query_first(d.\"Data\"::jsonb, 'strict $.\"displayName\"'", sql);
    }

    [Fact]
    public void FilterClause_Translates_ToSqlServerQuery_Test() {
        using var dbContext = CreateDbContext(builder => builder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=Indice.FilterClause.Test"));
        var filters = new List<FilterClause> {
            (FilterClause)"data.displayName::contains::κων",
            (FilterClause)"data.period.to::gt::(DateTime)2024-01-17T00:00:00+02:00",
        };
        var sql = dbContext.Dummies.Where(filters).ToQueryString();
        Assert.Contains("JSON_VALUE([d].[Data], N'$.displayName')", sql);
        Assert.Contains("CONVERT(datetime2, SWITCHOFFSET(CONVERT(datetimeoffset, JSON_VALUE([d].[Data], N'$.period.to')), '+00:00'))", sql);
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
    public async Task FilterClause_ComparesDates_InUtc_OnNonUtcSession_Test() {
        Assert.SkipWhen(_serviceProvider is null, $"Set the {ConnectionStringVariable} environment variable to run against PostgreSQL.");
        var cancellationToken = TestContext.Current.CancellationToken;
        DbContext.Dummies.AddRange(
            new Dummy { Name = "Utc", Data = new DummyItem { DisplayName = "Utc", BirthDate = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc) } },
            new Dummy { Name = "Unspecified", Data = new DummyItem { DisplayName = "Unspecified", BirthDate = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Unspecified) } }
        );
        await DbContext.SaveChangesAsync(cancellationToken);
        await DbContext.Database.OpenConnectionAsync(cancellationToken);
        try {
            await DbContext.Database.ExecuteSqlRawAsync("SET TIME ZONE 'Europe/Athens'", cancellationToken);
            var results = await DbContext.Dummies.Where([(FilterClause)"data.birthDate::gte::(DateTime)2024-01-17T00:00:00Z"]).ToListAsync(cancellationToken);
            Assert.Equal(["Unspecified", "Utc"], results.Select(x => x.Name).Order());
            results = await DbContext.Dummies.Where([(FilterClause)"data.birthDate::lte::(DateTime)2024-01-17T00:00:00Z"]).ToListAsync(cancellationToken);
            Assert.Equal(5, results.Count);
            results = await DbContext.Dummies.Where([(FilterClause)"data.birthDate::gt::(DateTime)2024-01-17T00:00:00Z"]).ToListAsync(cancellationToken);
            Assert.Empty(results);
            results = await DbContext.Dummies.Where([(FilterClause)"data.birthDate::gte::(DateTime)2024-01-17T02:00:00+02:00"]).ToListAsync(cancellationToken);
            Assert.Equal(2, results.Count);
            results = await DbContext.Dummies.Where([(FilterClause)"data.birthDate::gte::(DateTime)2024-01-17"]).ToListAsync(cancellationToken);
            Assert.Equal(2, results.Count);
            results = await DbContext.Dummies.Where([(FilterClause)"data.birthDate::eq::(DateTime)2024-01-17"]).ToListAsync(cancellationToken);
            Assert.Equal(2, results.Count);
            results = await DbContext.Dummies.Where([(FilterClause)"data.birthDate::in::(DateTime)2024-01-17T00:00:00Z,1981-01-28"]).ToListAsync(cancellationToken);
            Assert.Equal(3, results.Count);
            var boundary = new DateTime(2024, 1, 17, 0, 0, 0, DateTimeKind.Utc);
            results = await DbContext.Dummies.Where(x => JsonFunctions.CastToDate(JsonFunctions.JsonValue(x.Data!, "$.birthDate")) == boundary).ToListAsync(cancellationToken);
            Assert.Equal(["Unspecified", "Utc"], results.Select(x => x.Name).Order());
        } finally {
            await DbContext.Database.CloseConnectionAsync();
        }
    }

    [Fact]
    public async Task FilterClause_ReturnsNull_ForNonScalarJsonValues_OnPostgreSql_Test() {
        Assert.SkipWhen(_serviceProvider is null, $"Set the {ConnectionStringVariable} environment variable to run against PostgreSQL.");
        var cancellationToken = TestContext.Current.CancellationToken;
        var results = await DbContext.Dummies.Where([(FilterClause)"data.period::gt::(DateTime)2000-01-01"]).ToListAsync(cancellationToken);
        Assert.Empty(results);
        results = await DbContext.Dummies.Where([(FilterClause)"data.period::eq::{}"]).ToListAsync(cancellationToken);
        Assert.Empty(results);
        results = await DbContext.Dummies.Where(x => JsonFunctions.JsonValue(x.Data!, "$") != null).ToListAsync(cancellationToken);
        Assert.Empty(results);
        results = await DbContext.Dummies.Where(x => JsonFunctions.JsonValue(x.Data!, "$.displayName") == "Γιάννης Νές").ToListAsync(cancellationToken);
        Assert.Single(results);
        DbContext.Dummies.Add(new Dummy { ModernExtras = System.Text.Json.Nodes.JsonNode.Parse("""{"items":{"0":"matched"}}""") });
        await DbContext.SaveChangesAsync(cancellationToken);
        results = await DbContext.Dummies.Where(x => JsonFunctions.JsonValue(x.ModernExtras!, "$.items[0]") == "matched").ToListAsync(cancellationToken);
        Assert.Empty(results);
        results = await DbContext.Dummies.Where(x => JsonFunctions.JsonValue(x.ModernExtras!, "$.items.\"0\"") == "matched").ToListAsync(cancellationToken);
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
