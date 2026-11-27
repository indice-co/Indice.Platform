using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Server.Manager;
using Indice.Features.Identity.Server.Manager.Models;
using Microsoft.AspNetCore.Http.HttpResults;
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
        Assert.Equal(UserRoleChangeType.Added, added.ChangeType);
        Assert.Equal("Developer", added.Role);
        Assert.False(added.IsGlobalAdmin);

        _events.Clear();
        Assert.False((await manager.AddToRoleAsync(user, "DEVELOPER")).Succeeded);
        Assert.Empty(_events.OfType<UserRolesChangedEvent>());

        Assert.True((await manager.RemoveFromRoleAsync(user, "developer")).Succeeded);
        var removed = Assert.Single(_events.OfType<UserRolesChangedEvent>());
        Assert.Equal(user.Id, removed.User.Id);
        Assert.Equal(UserRoleChangeType.Removed, removed.ChangeType);
        Assert.Equal("Developer", removed.Role);

        _events.Clear();
        Assert.False((await manager.RemoveFromRoleAsync(user, "developer")).Succeeded);
        Assert.Empty(_events.OfType<UserRolesChangedEvent>());
    }

    [Fact]
    public async Task MultipleRoleChanges_PublishOneEventPerRole_AndNoEventForEmptyChanges() {
        var manager = _services.GetRequiredService<ExtendedUserManager<User>>();
        await CreateRoleAsync("Developer");
        await CreateRoleAsync("Reviewer");
        var user = new User("multiple@example.com");
        Assert.True((await manager.CreateAsync(user)).Succeeded);
        _events.Clear();

        Assert.True((await manager.AddToRolesAsync(user, ["developer", "reviewer"])).Succeeded);
        var added = _events.OfType<UserRolesChangedEvent>().ToArray();
        Assert.Equal(2, added.Length);
        Assert.All(added, e => { Assert.Equal(user.Id, e.User.Id); Assert.Equal(UserRoleChangeType.Added, e.ChangeType); });
        Assert.Equal(new[] { "Developer", "Reviewer" }, added.Select(e => e.Role).OrderBy(x => x));

        _events.Clear();
        Assert.True((await manager.AddToRolesAsync(user, [])).Succeeded);
        Assert.True((await manager.RemoveFromRolesAsync(user, [])).Succeeded);
        Assert.False((await manager.AddToRolesAsync(user, ["developer"])).Succeeded);
        Assert.Empty(_events.OfType<UserRolesChangedEvent>());

        Assert.True((await manager.RemoveFromRolesAsync(user, ["DEVELOPER", "REVIEWER"])).Succeeded);
        var removed = _events.OfType<UserRolesChangedEvent>().ToArray();
        Assert.Equal(2, removed.Length);
        Assert.All(removed, e => { Assert.Equal(user.Id, e.User.Id); Assert.Equal(UserRoleChangeType.Removed, e.ChangeType); });
        Assert.Equal(new[] { "Developer", "Reviewer" }, removed.Select(e => e.Role).OrderBy(x => x));
    }

    [Fact]
    public async Task CreateUser_WithInitialRoles_DoesNotPublishRoleChanges() {
        var manager = _services.GetRequiredService<ExtendedUserManager<User>>();
        var role = await CreateRoleAsync("Developer");
        var user = new User("initial@example.com");
        user.Roles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });

        Assert.True((await manager.CreateAsync(user)).Succeeded);

        var created = Assert.IsType<UserCreatedEvent>(Assert.Single(_events));
        Assert.Equal(user.Id, created.User.Id);
        Assert.Equal(new[] { "Developer" }, await manager.GetRolesAsync(user));

        _events.Clear();
        Assert.True((await manager.RemoveFromRoleAsync(user, "Developer")).Succeeded);
        var changed = Assert.Single(_events.OfType<UserRolesChangedEvent>());
        Assert.Equal(UserRoleChangeType.Removed, changed.ChangeType);
        Assert.Equal("Developer", changed.Role);
    }

    [Fact]
    public async Task UpdateUser_GlobalAdminChanges_PublishEventsOnlyForSuccessfulChanges() {
        var manager = _services.GetRequiredService<ExtendedUserManager<User>>();
        var dbContext = _services.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();
        var user = new User("admin-change@example.com") { Email = "admin-change@example.com" };
        Assert.True((await manager.CreateAsync(user)).Succeeded);
        _events.Clear();
        var request = new UpdateUserRequest { UserName = user.UserName, Email = user.Email, IsAdmin = true };

        var result = await UserHandlers.UpdateUser(dbContext, manager, user.Id, request);
        Assert.IsType<Ok<SingleUserInfo>>(result.Result);
        var added = Assert.Single(_events.OfType<UserRolesChangedEvent>());
        Assert.Equal(user.Id, added.User.Id);
        Assert.True(added.User.Admin);
        Assert.Equal(UserRoleChangeType.Added, added.ChangeType);
        Assert.Equal(UserRolesChangedEvent.GlobalAdminRole, added.Role);
        Assert.True(added.IsGlobalAdmin);

        _events.Clear();
        result = await UserHandlers.UpdateUser(dbContext, manager, user.Id, request);
        Assert.IsType<Ok<SingleUserInfo>>(result.Result);
        Assert.Empty(_events.OfType<UserRolesChangedEvent>());

        _events.Clear();
        request.IsAdmin = false;
        request.UserName = "invalid user name";
        result = await UserHandlers.UpdateUser(dbContext, manager, user.Id, request);
        Assert.IsType<ValidationProblem>(result.Result);
        Assert.Empty(_events.OfType<UserRolesChangedEvent>());

        request.UserName = "admin-change@example.com";
        user.Admin = true;
        result = await UserHandlers.UpdateUser(dbContext, manager, user.Id, request);
        Assert.IsType<Ok<SingleUserInfo>>(result.Result);
        var removed = Assert.Single(_events.OfType<UserRolesChangedEvent>());
        Assert.Equal(user.Id, removed.User.Id);
        Assert.False(removed.User.Admin);
        Assert.Equal(UserRoleChangeType.Removed, removed.ChangeType);
        Assert.Equal(UserRolesChangedEvent.GlobalAdminRole, removed.Role);
        Assert.True(removed.IsGlobalAdmin);
    }

    private async Task<Role> CreateRoleAsync(string name) {
        var role = new Role(name);
        Assert.True((await _services.GetRequiredService<RoleManager<Role>>().CreateAsync(role)).Succeeded);
        return role;
    }

    public void Dispose() => _services.Dispose();
}
