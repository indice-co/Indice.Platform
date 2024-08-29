﻿using Indice.EntityFrameworkCore;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Services.Tests;

public class FilterClauseQueryableExtensionTests : IAsyncLifetime
{
    public FilterClauseQueryableExtensionTests() {
        var inMemorySettings = new Dictionary<string, string> {
            ["ConnectionStrings:TestDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=Indice.FilterClause.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true",
            //...populate as needed for the test
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        var collection = new ServiceCollection()
            .AddDbContext<DummyDbContext>(builder => builder.UseSqlServer(configuration.GetConnectionString("TestDb")), ServiceLifetime.Singleton)
            .AddSingleton(configuration);
        ServiceProvider = collection.BuildServiceProvider();
    }

    public ServiceProvider ServiceProvider { get; }

    [Fact]
    public async Task FilterClause_Translates_ToDatabaseQuery_Test() {
        var dbContext = ServiceProvider.GetRequiredService<DummyDbContext>();
        var filters = new List<FilterClause> {
            //(FilterClause)"extras.id::eq::(integer)15",
            (FilterClause)"data.displayName::contains::κων",
            (FilterClause)$"data.period.to::gt::(DateTime){DateTime.Now:yyyy-MM-dd}",
            (FilterClause)"metadata.NAME::eq::Thanos",
            //(FilterClause)"data.enabled::eq::(boolean)true",
            //(FilterClause)"name::eq::Κωνσταντίνος",
        };
        var query = dbContext.Dummies.Where(filters);
        var results = await query.ToListAsync();
        Assert.Single(results);
    }

    [Fact]
    public async Task ToResultset_Translates_DynamicJsonPaths_Sort_Test() {
        var dbContext = ServiceProvider.GetRequiredService<DummyDbContext>();
        var query = dbContext.Dummies;
        var results = await query.ToResultSetAsync(new ListOptions { Sort = "data.displayName,name" });
        results = await query.ToResultSetAsync(new ListOptions { Sort = "data.displayName" });
        results = await query.ToResultSetAsync(new ListOptions { Sort = "(integer)data.order" });
        results = await query.ToResultSetAsync(new ListOptions { Sort = "(datetime)data.period.from+" });
        results = await query.ToResultSetAsync(new ListOptions { Sort = "(datetime)data.birthDate-" });
        results = await query.ToResultSetAsync(new ListOptions { Sort = "(number)data.Balance-" });
        results = await query.ToResultSetAsync(new ListOptions { Sort = "(boolean)data.enabled-" });
        Assert.True(true);
    }

    [Fact]
    public async Task ToResultset_Translates_DynamicJsonPaths_MultiSort_Test() {
        var dbContext = ServiceProvider.GetRequiredService<DummyDbContext>();
        var query = dbContext.Dummies.AsQueryable();
        var options = new ListOptions { Sort = "name-,data.displayName" };
        foreach (var sorting in options.GetSortings()) {
            query = query.OrderBy(sorting, append: true);
        }
        var results = await query.ToResultSetAsync(options.Page ?? 1, options.Size ?? 100);
        Assert.True(true);
    }

    public async Task InitializeAsync() {
        var dbContext = ServiceProvider.GetRequiredService<DummyDbContext>();
        await dbContext.SeedAsync();
    }

    public async Task DisposeAsync() {
        var dbContext = ServiceProvider.GetRequiredService<DummyDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await ServiceProvider.DisposeAsync();
    }
}

public class DummyDbContext : DbContext
{
    public DummyDbContext(DbContextOptions<DummyDbContext> options) : base(options) {
        
    }

    public async Task<bool> SeedAsync() {
        if (!await Database.EnsureCreatedAsync()) {
            return false;
        }
        Dummies.AddRange(
                new Dummy { Name = "Κωνσταντίνος", Extras = new { Id = 5 }, Metadata = new Dictionary<string, string> { ["NAME"] = "Thanos", ["Surname"] = "Panos" }, Data = new DummyItem { DisplayName = "Κωνσταντίνος Θέρης", Enabled = true, Order = 7, BirthDate = new DateTime(1981, 01, 28), Balance = 100.0, Period = new Period { From = DateTime.Now.AddDays(-10), To = DateTime.Now.AddDays(10) } } },
                new Dummy { Name = "Γιώργος", Extras = new { Id = 15 }, Data = new DummyItem { DisplayName = "Γιώργος Τζάς", Enabled = false, Order = -14, BirthDate = new DateTime(1989, 10, 24), Balance = 360.23 } },
                new Dummy { Name = "Γιάννης", Extras = new { Id = 7 }, Metadata = new Dictionary<string, string> { ["NAME"] = "Thanos" }, Data = new DummyItem { DisplayName = "Γιάννης Νές", Enabled = true, Order = 2, BirthDate = new DateTime(1971, 12, 1), Balance = 1260.23 } }
            );
        await SaveChangesAsync();
        return true;
    }

    public DbSet<Dummy> Dummies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Dummy>().HasKey(x => x.Id);
        modelBuilder.Entity<Dummy>().Property(x => x.Data).HasJsonConversion();
        modelBuilder.Entity<Dummy>().Property(x => x.Extras).HasJsonConversion();
        modelBuilder.Entity<Dummy>().Property(x => x.Metadata).HasJsonConversion();
        modelBuilder.ApplyJsonFunctions();
        base.OnModelCreating(modelBuilder);
    }
}


public class DummyItem
{
    public DateTime? BirthDate { get; set; }
    public int Order { get; set; }
    public bool Enabled { get; set; }
    public string DisplayName { get; set; }
    public double Balance { get; set; }
    public Period Period { get; set; } = new Period();
}

public class Dummy
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public dynamic Extras { get; set; }
    public DummyItem Data { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}
