using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Server.Manager;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Features.Identity.Tests;
public class UserHandlersTests : IAsyncLifetime
{
    private readonly ServiceProvider _serviceProvider;

    public UserHandlersTests() {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:TestDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=Indice.FilterClause.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true",
        }).Build();
        var services = new ServiceCollection();
        // configure dependencies
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<ExtendedIdentityDbContext<User, Role>>(builder => builder.UseSqlServer(configuration.GetConnectionString("TestDb")));
        services.AddIdentity<User, Role>()
                   .AddExtendedUserManager()
                   .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                   .AddExtendedSignInManager()
                   .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                   .AddExtendedPhoneNumberTokenProvider(configuration)
                   .AddIdentityMessageDescriber();
        services.AddLogging();
        services.AddLocalization();
        services.AddDefaultPlatformEventService();
        services.AddPlatformEventHandler<UserCreatedEvent, UserCreatedAssetionHanbdler>();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task CreateUserHandler_ShouldEmit_UserCreatedEvent_WithPopulatedClaims_Test() {

        var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var identityDbContext = _serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();

        //seed database
        identityDbContext.Roles.Add(new Role("Developer") { NormalizedName = "DEVELOPER" });
        await identityDbContext.SaveChangesAsync();

        // execute
        _ = await UserHandlers.CreateUser(userManager, identityDbContext, new Server.Manager.Models.CreateUserRequest {
            UserName = "john.doe@indice.gr",
            Email = "john.doe@indice.gr",
            Password = "password",
            BypassPasswordValidation = true,
            FirstName = "John",
            LastName = "Doe",
            Claims = [
                new() { Type = "customer_code", Value = "000001" },
                new() { Type = "locale", Value = "el" }
            ],
            Roles = ["Developer"]
        });
        Assert.True(true);
    }



    [Fact]
    public async Task GetUsers_Test() {

        var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
        //seed database
        var identityDbContext = _serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();

        //seed database
        identityDbContext.Roles.Add(new Role("Developer") { NormalizedName = "DEVELOPER" });
        await identityDbContext.SaveChangesAsync();

        // execute
        _ = await UserHandlers.CreateUser(userManager, identityDbContext, new Server.Manager.Models.CreateUserRequest {
            UserName = "john.doe@indice.gr",
            Email = "john.doe@indice.gr",
            Password = "password",
            BypassPasswordValidation = true,
            FirstName = "John",
            LastName = "Doe",
            Claims = [
                new() { Type = "customer_code", Value = "000001" },
                new() { Type = "locale", Value = "el" }
            ],
            Roles = ["Developer"]
        });

        ListOptions options = new() {
            Page = 1,
            Size = 10
        };
        UserListFilter filter = new();
        // execute
        _ = await UserHandlers.GetUsers(identityDbContext, options, filter, ["locale", "customer_code"]);
        Assert.True(true);
    }


    public async Task InitializeAsync() {
        var dbContext = _serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() {
        var dbContext = _serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();
        await dbContext.Database.EnsureDeletedAsync();
        await _serviceProvider.DisposeAsync();
    }

    public class UserCreatedAssetionHanbdler : IPlatformEventHandler<UserCreatedEvent>
    {
        public Task Handle(UserCreatedEvent @event, PlatformEventArgs args) {
            args.ThrowOnError = true;
            Assert.Equal(4, @event.User.Claims.Count);
            return Task.CompletedTask;
        }
    }
}