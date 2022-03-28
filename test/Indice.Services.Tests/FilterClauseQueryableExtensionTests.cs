using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.EntityFrameworkCore;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Indice.Services.Tests
{
    public class FilterClauseQueryableExtensionTests : IDisposable
    {
        public FilterClauseQueryableExtensionTests() {
            var inMemorySettings = new Dictionary<string, string> {
                ["ConnectionStrings:TestDb"] = "Server=(localdb)\\MSSQLLocalDB;Database=Indice.FilterClause.Test;Trusted_Connection=True;MultipleActiveResultSets=true",
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
                (FilterClause)"data.displayName::contains::κων",
                (FilterClause)$"data.period.to::gt::(DateTime){DateTime.Now:yyyy-MM-dd}",
                (FilterClause)"metadata.NAME::eq::Thanos",
                //(FilterClause)"name::eq::Κωνσταντίνος",
            };
            var query = dbContext.Dummies.Where(filters);
            var results = await query.ToListAsync();
            Assert.Single(results);
        }

        [Fact]
        public async Task ToResultset_Translates_DynamicJsonPaths_Test() {
            var dbContext = ServiceProvider.GetRequiredService<DummyDbContext>();
            var options = new ListOptions() {
                Sort = "data.displayName-,name"
            };
            var query = dbContext.Dummies;
            var results = await query.ToResultSetAsync(options);
            Assert.True(true);
        }

        public void Dispose() {
            var dbContext = ServiceProvider.GetRequiredService<DummyDbContext>();
            dbContext.Database.EnsureDeleted();
            ServiceProvider.Dispose();
        }
    }

    public class DummyDbContext : DbContext
    {
        public DummyDbContext(DbContextOptions<DummyDbContext> options) : base(options) {
            if (Database.EnsureCreated()) {
                Dummies.AddRange(
                    new Dummy { Name = "Κωνσταντίνος", Metadata = new Dictionary<string, string> { ["NAME"] = "Thanos", ["Surname"] = "Panos" } , Data = new DummyItem { DisplayName = "Κωνσταντίνος Θέρης", Enabled = true, Order = 1, TheDate = new DateTime(1981, 01, 28), Balance = 100.0, Period = new Period { From = DateTime.Now.AddDays(-10), To = DateTime.Now.AddDays(10) } } },
                    new Dummy { Name = "Γιώργος", Data = new DummyItem { DisplayName = "Γιώργος Τζάς", Enabled = false, Order = 2, TheDate = new DateTime(1989, 10, 24), Balance = 360.23 } },
                    new Dummy { Name = "Γιάννης", Metadata = new Dictionary<string, string> { ["NAME"] = "Thanos" }, Data = new DummyItem { DisplayName = "Γιάννης Νές", Enabled = true, Order = 3, TheDate = new DateTime(1971, 12, 1), Balance = 1260.23 } }
                    );
                SaveChanges();
            }
        }

        public DbSet<Dummy> Dummies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Dummy>().HasKey(x => x.Id);
            modelBuilder.Entity<Dummy>().Property(x => x.Data).HasJsonConversion();
            modelBuilder.Entity<Dummy>().Property(x => x.Metadata).HasJsonConversion();
            modelBuilder.ApplyJsonFunctions();
            base.OnModelCreating(modelBuilder);
        }
    }

    
    public class DummyItem
    {
        public DateTime? TheDate { get; set; }
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
        public DummyItem Data { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}
