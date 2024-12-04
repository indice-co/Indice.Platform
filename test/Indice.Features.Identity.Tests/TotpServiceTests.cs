﻿using System.Security.Claims;
using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Totp;
using Indice.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Indice.Features.Identity.Tests;

public class TotpServiceTests
{
    public TotpServiceTests() {
        var builder = new WebHostBuilder();
        builder.ConfigureAppConfiguration(builder => {
            builder.AddInMemoryCollection(new Dictionary<string, string> {
                ["Totp:EnableDeveloperTotp"] = "true"
            });
        });
        builder.ConfigureServices(services => {
            services.TryAddTransient<IPlatformEventService, DefaultPlatformEventService>();
            var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
            services.AddTransient<IUserStateProvider<User>, UserStateProviderNoop>();
            services.AddTotpServiceFactory(configuration)
                    .AddSmsServiceNoop()
                    .AddPushNotificationServiceNoop()
                    .AddLocalization()
                    .AddDistributedMemoryCache()
                    .AddDbContext<ExtendedIdentityDbContext<User, Role>>(builder => builder.UseInMemoryDatabase(Guid.NewGuid().ToString()))
                    .AddIdentity<User, Role>()
                    .AddExtendedUserManager()
                    .AddExtendedSignInManager()
                    .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                    .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                    .AddExtendedPhoneNumberTokenProvider(configuration)
                    .AddIdentityMessageDescriber<IdentityMessageDescriber>();
        });
        builder.Configure(app => { });
        TestServer = new TestServer(builder);
    }

    public TestServer TestServer { get; }

    [Fact]
    public async Task Can_Generate_And_Verify_Code_Using_DeveloperTotpService() {
        const string DEVELOPER_TOTP = "763498";
        var random = new Random(Guid.NewGuid().GetHashCode()).Next();
        var email = $"dev_{random}@indice.gr";
        var user = new User {
            CreateDate = DateTimeOffset.UtcNow,
            Email = email,
            Id = Guid.NewGuid().ToString(),
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = email
        };
        var userManager = TestServer.Services.GetRequiredService<ExtendedUserManager<User>>();
        // Create a new user.
        var identityResult = await userManager.CreateAsync(user);
        Assert.True(identityResult.Succeeded);
        // Assign the user the developer_totp claim.
        identityResult = await userManager.AddClaimAsync(user, new Claim(BasicClaimTypes.DeveloperTotp, DEVELOPER_TOTP, ClaimValueTypes.Integer32));
        Assert.True(identityResult.Succeeded);
        var roleManager = TestServer.Services.GetRequiredService<RoleManager<Role>>();
        // Check if 'Developer' role exists in the store, otherwise create it.
        var developerRole = await roleManager.FindByNameAsync(BasicRoleNames.Developer);
        if (developerRole is null) {
            identityResult = await roleManager.CreateAsync(new Role(BasicRoleNames.Developer));
            Assert.True(identityResult.Succeeded);
        }
        // Add user to 'Developer' role.
        identityResult = await userManager.AddToRoleAsync(user, BasicRoleNames.Developer);
        Assert.True(identityResult.Succeeded);
        var totpServiceFactory = TestServer.Services.GetRequiredService<TotpServiceFactory>();
        var totpService = totpServiceFactory.Create<User>();
        var totpResult = await totpService.SendAsync(totp => totp
            .ToUser(user)
            .WithMessage("Your one-time code is {0}. It is valid for 2 minutes.")
            .UsingSms()
            .WithSubject("OTP")
        );
        Assert.True(totpResult.Success);
        totpResult = await totpService.VerifyAsync(user, DEVELOPER_TOTP);
        Assert.True(totpResult.Success);
    }

    [Fact]
    public async Task Can_Generate_Code_Using_SecurityTokenTotpService() {
        var totpServiceFactory = TestServer.Services.GetRequiredService<TotpServiceFactory>();
        var totpService = totpServiceFactory.Create();
        var securityToken = Guid.NewGuid().ToString();
        var totpResult = await totpService.SendAsync(totp => totp
            .UseSecurityToken(securityToken)
            .WithMessage("Your one-time code is {0}. It is valid for 2 minutes.")
            .ToPhoneNumber("699XXXXXXX")
            .UsingSms()
            .WithSubject("OTP")
        );
        Assert.True(totpResult.Success);
    }
}
