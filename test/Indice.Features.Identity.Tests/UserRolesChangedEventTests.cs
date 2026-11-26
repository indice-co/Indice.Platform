using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Events;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Indice.Features.Identity.Tests;

public class UserRolesChangedEventTests : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly List<IPlatformEvent> _events = [];

    public UserRolesChangedEventTests() {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var events = new Mock<IPlatformEventService>();
        events.Setup(x => x.Publish(It.IsAny<IPlatformEvent>()))
              .Callback<IPlatformEvent>(e => _events.Add(e))
              .Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(events.Object);
        services.AddLogging();
        services.AddLocalization();
        services.AddDbContext<ExtendedIdentityDbContext<User, Role>>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentity<User, Role>()
                .AddExtendedUserManager()
                .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                .AddIdentityMessageDescriber();
        _services = services.BuildServiceProvider();
    }

    [Fact]
    public async Task SingleRoleChanges_PublishActualNames_OnlyOnSuccess() {
        var manager = _services.GetRequiredService<ExtendedUserManager<User>>();
        await CreateRoleAsync("Developer");
        var user = new User("single@example.com");
        Assert.True((await manager.CreateAsync(user)).Succeeded);
        Assert.Empty(_events.OfType<UserRolesChangedEvent>());
        _events.Clear();

        Assert.True((await manager.AddToRoleAsync(user, "developer")).Succeeded);
        var added = Assert.Single(_events.OfType<UserRolesChangedEvent>());
        Assert.Equal(user.Id, added.User.Id);
        Assert.Equal(new[] { "Developer" }, added.AddedRoles);
        Assert.Empty(added.RemovedRoles);

        _events.Clear();
        Assert.False((await manager.AddToRoleAsync(user, "DEVELOPER")).Succeeded);
        Assert.Empty(_events.OfType<UserRolesChangedEvent>());

        Assert.True((await manager.RemoveFromRoleAsync(user, "developer")).Succeeded);
        var removed = Assert.Single(_events.OfType<UserRolesChangedEvent>());
        Assert.Equal(user.Id, removed.User.Id);
        Assert.Empty(removed.AddedRoles);
        Assert.Equal(new[] { "Developer" }, removed.RemovedRoles);

        _events.Clear();
        Assert.False((await manager.RemoveFromRoleAsync(user, "developer")).Succeeded);
        Assert.Empty(_events.OfType<UserRolesChangedEvent>());
    }

    [Fact]
    public async Task MultipleRoleChanges_PublishOneEventPerOperation_AndNoEventForEmptyChanges() {
        var manager = _services.GetRequiredService<ExtendedUserManager<User>>();
        await CreateRoleAsync("Developer");
        await CreateRoleAsync("Reviewer");
        var user = new User("multiple@example.com");
        Assert.True((await manager.CreateAsync(user)).Succeeded);
        _events.Clear();

        Assert.True((await manager.AddToRolesAsync(user, ["developer", "reviewer"])).Succeeded);
        var added = Assert.Single(_events.OfType<UserRolesChangedEvent>());
        Assert.Equal(new[] { "Developer", "Reviewer" }, added.AddedRoles.OrderBy(x => x));
        Assert.Empty(added.RemovedRoles);

        _events.Clear();
        Assert.True((await manager.AddToRolesAsync(user, [])).Succeeded);
        Assert.True((await manager.RemoveFromRolesAsync(user, [])).Succeeded);
        Assert.False((await manager.AddToRolesAsync(user, ["developer"])).Succeeded);
        Assert.Empty(_events.OfType<UserRolesChangedEvent>());

        Assert.True((await manager.RemoveFromRolesAsync(user, ["DEVELOPER", "REVIEWER"])).Succeeded);
        var removed = Assert.Single(_events.OfType<UserRolesChangedEvent>());
        Assert.Empty(removed.AddedRoles);
        Assert.Equal(new[] { "Developer", "Reviewer" }, removed.RemovedRoles.OrderBy(x => x));
    }

    [Fact]
    public async Task CreateUser_WithInitialRoles_PublishesAddedRoles() {
        var manager = _services.GetRequiredService<ExtendedUserManager<User>>();
        var role = await CreateRoleAsync("Developer");
        var user = new User("initial@example.com");
        user.Roles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });

        Assert.True((await manager.CreateAsync(user)).Succeeded);

        var changed = Assert.Single(_events.OfType<UserRolesChangedEvent>());
        Assert.Equal(user.Id, changed.User.Id);
        Assert.Equal(new[] { "Developer" }, changed.AddedRoles);
        Assert.Empty(changed.RemovedRoles);
        Assert.IsType<UserCreatedEvent>(_events[0]);
    }

    private async Task<Role> CreateRoleAsync(string name) {
        var role = new Role(name);
        Assert.True((await _services.GetRequiredService<RoleManager<Role>>().CreateAsync(role)).Succeeded);
        return role;
    }

    public void Dispose() => _services.Dispose();
}
