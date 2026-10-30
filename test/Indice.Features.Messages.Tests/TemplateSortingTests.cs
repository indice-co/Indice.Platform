using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Features.Messages.Tests;

public class TemplateSortingTests : IAsyncLifetime
{
    private readonly ServiceProvider _serviceProvider;

    public TemplateSortingTests() {
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=MessagesDb.TemplateSortingTest_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true";
        _serviceProvider = new ServiceCollection()
            .AddDbContext<CampaignsDbContext>(builder => builder.UseSqlServer(connectionString))
            .AddTransient(serviceProvider => new DatabaseSchemaNameResolver("cmp"))
            .AddTransient<IUserNameAccessor, UserNameAccessorNoOp>()
            .AddTransient<UserNameAccessorAggregate>()
            .BuildServiceProvider();
    }

    public async ValueTask InitializeAsync() => await _serviceProvider.GetRequiredService<CampaignsDbContext>().Database.EnsureCreatedAsync();

    public async ValueTask DisposeAsync() {
        await _serviceProvider.GetRequiredService<CampaignsDbContext>().Database.EnsureDeletedAsync();
        await _serviceProvider.DisposeAsync();
    }

    [Theory]
    [InlineData("updatedAt-", new[] { "new", "edited", "old" })]
    [InlineData("updatedAt", new[] { "old", "edited", "new" })]
    public async Task GetList_SortsByUpdatedAt_FallingBackToCreatedAt(string sort, string[] expected) {
        var now = DateTimeOffset.UtcNow;
        var db = _serviceProvider.GetRequiredService<CampaignsDbContext>();
        await SeedAsync(db, "old", createdAt: now.AddDays(-10), updatedAt: null);
        await SeedAsync(db, "edited", createdAt: now.AddDays(-20), updatedAt: now.AddDays(-5));
        // never edited, but the newest: must not sort behind edited templates
        await SeedAsync(db, "new", createdAt: now.AddDays(-1), updatedAt: null);

        var result = await new TemplateService(db).GetList(new ListOptions<TemplateListFilter> { Page = 1, Size = 10, Sort = sort });

        Assert.Equal(expected, result.Items.Select(x => x.Name));
    }

    private static async Task SeedAsync(CampaignsDbContext db, string name, DateTimeOffset createdAt, DateTimeOffset? updatedAt) {
        var id = Guid.NewGuid();
        db.Templates.Add(new DbTemplate { Id = id, Name = name, CreatedBy = "test" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        // the context stamps the audit dates on save, so set them afterwards
        await db.Templates.Where(x => x.Id == id).ExecuteUpdateAsync(x => x
            .SetProperty(t => t.CreatedAt, createdAt)
            .SetProperty(t => t.UpdatedAt, updatedAt), TestContext.Current.CancellationToken);
    }
}
