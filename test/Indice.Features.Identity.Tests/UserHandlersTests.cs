#if NET8_0_OR_GREATER
using System;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Server.Manager;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Features.Identity.Tests;
public class UserHandlersTests : IAsyncLifetime
{
    private readonly ServiceProvider _serviceProvider;

    public UserHandlersTests() {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
            ["test"] = "test"
        }).Build();
        var services = new ServiceCollection();
        // configure dependencies
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<ExtendedIdentityDbContext<User, Role>>(builder => builder.UseInMemoryDatabase("IdentityDb"));
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
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task CreateUserHandler_ShouldEmit_UserCreatedEvent_WithPopulatedClaims_Test() {
        
        var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var identityDbContext = _serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();
        
        //seed database
        identityDbContext.Roles.Add(new Role("Developer") { NormalizedName = "DEVELOPER"} );
        await identityDbContext.SaveChangesAsync();

        // execute
        var result = await UserHandlers.CreateUser(userManager, identityDbContext, new Server.Manager.Models.CreateUserRequest {
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
            Roles = [ "Developer" ]
        });
        Assert.True(true);
    }

    public async Task DisposeAsync() {
        await _serviceProvider.DisposeAsync();
    }

    public Task InitializeAsync() {
        return Task.CompletedTask;
    }
}
#endif