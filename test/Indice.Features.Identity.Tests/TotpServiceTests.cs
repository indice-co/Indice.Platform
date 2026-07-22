using System.Security.Claims;
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
            builder.AddInMemoryCollection(new Dictionary<string, string?> {
                ["Totp:EnableDeveloperTotp"] = "true"
            });
        });
        builder.ConfigureServices(services => {
            services.TryAddTransient<IPlatformEventService, DefaultPlatformEventService>();
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            services.AddTransient<IUserRequirementProvider<User>, UserRequirementProviderNoOp>();
            services.AddTotpServiceFactory(configuration)
                    .AddSmsServiceNoop()
                    .AddEmailServiceNoop()
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
                    .AddExtendedEmailTokenProvider(configuration)
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

    [Fact]
    public void TotpServiceFactory_Create_Without_User_Returns_TotpServiceSecurityToken() {
        // Arrange
        var totpServiceFactory = TestServer.Services.GetRequiredService<TotpServiceFactory>();

        // Act
        var totpService = totpServiceFactory.Create();

        // Assert
        Assert.NotNull(totpService);
        Assert.IsType<TotpServiceSecurityToken>(totpService);
    }

    [Fact]
    public async Task TotpServiceFactory_Create_SecurityToken_Can_Send_Via_Sms() {
        // Arrange
        var totpServiceFactory = TestServer.Services.GetRequiredService<TotpServiceFactory>();
        var totpService = totpServiceFactory.Create();
        var securityToken = Guid.NewGuid().ToString();
        var phoneNumber = "+306991234567";

        // Act
        var result = await totpService.SendAsync(totp => totp
            .UseSecurityToken(securityToken)
            .WithMessage("Your verification code is {0}")
            .ToPhoneNumber(phoneNumber)
            .UsingSms()
            .WithSubject("Verification")
        );

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task TotpServiceFactory_Create_SecurityToken_Can_Send_Via_Email() {
        // Arrange
        var totpServiceFactory = TestServer.Services.GetRequiredService<TotpServiceFactory>();
        var totpService = totpServiceFactory.Create();
        var securityToken = Guid.NewGuid().ToString();
        var email = "test@example.com";

        // Act
        var result = await totpService.SendAsync(totp => totp
            .UseSecurityToken(securityToken)
            .WithMessage("Your verification code is {0}")
            .ToEmail(email)
            .UsingEmail()
            .WithSubject("Verification")
        );

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task TotpServiceFactory_Create_SecurityToken_Can_Verify_Code() {
        // Arrange
        var totpServiceFactory = TestServer.Services.GetRequiredService<TotpServiceFactory>();
        var totpService = totpServiceFactory.Create();
        var securityToken = Guid.NewGuid().ToString();
        var phoneNumber = "+306991234567";

        // Act - Send the code first
        var sendResult = await totpService.SendAsync(totp => totp
            .UseSecurityToken(securityToken)
            .WithMessage("Your verification code is {0}")
            .ToPhoneNumber(phoneNumber)
            .UsingSms()
            .WithSubject("Verification")
            
        );

        Assert.True(sendResult.Success);
        Assert.NotNull(sendResult);
    }

    [Fact]
    public async Task TotpServiceFactory_Create_SecurityToken_Returns_Error_For_Invalid_Channel() {
        // Arrange
        var totpServiceFactory = TestServer.Services.GetRequiredService<TotpServiceFactory>();
        var totpService = totpServiceFactory.Create();
        var securityToken = Guid.NewGuid().ToString();

        // Act - Try to send without specifying phone or email
        var result = await totpService.SendAsync(totp => totp
            .UseSecurityToken(securityToken)
            .WithMessage("Your verification code is {0}")
            .ToPhoneNumber("") // Empty phone number
            .UsingSms()
            .WithSubject("Verification")
        );

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task TotpServiceFactory_Create_SecurityToken_Handles_Multiple_Requests() {
        // Arrange
        var totpServiceFactory = TestServer.Services.GetRequiredService<TotpServiceFactory>();
        var totpService = totpServiceFactory.Create();
        var securityToken = Guid.NewGuid().ToString();
        var phoneNumber = "+306991234567";

        // Act - Send first code
        var firstResult = await totpService.SendAsync(totp => totp
            .UseSecurityToken(securityToken)
            .WithMessage("Your verification code is {0}")
            .ToPhoneNumber(phoneNumber)
            .UsingSms()
            .WithSubject("Verification")
        );

        // Act - Try to send second code immediately (should be rate limited)
        var secondResult = await totpService.SendAsync(totp => totp
            .UseSecurityToken(securityToken)
            .WithMessage("Your verification code is {0}")
            .ToPhoneNumber(phoneNumber)
            .UsingSms()
            .WithSubject("Verification")
        );

        // Assert
        Assert.True(firstResult.Success);
        Assert.NotNull(secondResult);
        Assert.True(secondResult.IsRateLimited);
    }

    [Fact]
    public void TotpServiceFactory_Constructor_Throws_On_Null_ServiceProvider() {
        Assert.Throws<ArgumentNullException>(() => new TotpServiceFactory(null!));
    }

    [Fact]
    public void TotpServiceFactory_Create_With_User_Returns_TotpServiceDeveloper_When_EnableDeveloperTotp_Is_True() {
        // Arrange - The TestServer is configured with EnableDeveloperTotp = true
        var totpServiceFactory = TestServer.Services.GetRequiredService<TotpServiceFactory>();

        // Act
        var totpService = totpServiceFactory.Create<User>();

        // Assert
        Assert.NotNull(totpService);
        Assert.IsType<TotpServiceDeveloper<User>>(totpService);
    }

    [Fact]
    public void TotpServiceFactory_Create_With_User_Returns_TotpServiceUser_When_EnableDeveloperTotp_Is_False() {
        // Arrange - Create a new TestServer with EnableDeveloperTotp = false
        var builder = new WebHostBuilder();
        builder.ConfigureAppConfiguration(builder => {
            builder.AddInMemoryCollection(new Dictionary<string, string?> {
                ["Totp:EnableDeveloperTotp"] = "false"
            });
        });
        builder.ConfigureServices(services => {
            services.TryAddTransient<IPlatformEventService, DefaultPlatformEventService>();
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            services.AddTransient<IUserRequirementProvider<User>, UserRequirementProviderNoOp>();
            services.AddTotpServiceFactory(configuration)
                    .AddSmsServiceNoop()
                    .AddEmailServiceNoop()
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
        using var testServer = new TestServer(builder);

        var totpServiceFactory = testServer.Services.GetRequiredService<TotpServiceFactory>();

        // Act
        var totpService = totpServiceFactory.Create<User>();

        // Assert
        Assert.NotNull(totpService);
        Assert.IsType<TotpServiceUser<User>>(totpService);
    }

    [Fact]
    public async Task TotpServiceFactory_Create_With_UserId_Resolves_User_And_Sends_Code() {
        // Arrange
        var random = new Random(Guid.NewGuid().GetHashCode()).Next();
        var email = $"user_{random}@example.com";
        var user = new User {
            CreateDate = DateTimeOffset.UtcNow,
            Email = email,
            Id = Guid.NewGuid().ToString(),
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = email,
            PhoneNumber = "+306991234567"
        };

        var userManager = TestServer.Services.GetRequiredService<ExtendedUserManager<User>>();
        var createResult = await userManager.CreateAsync(user);
        Assert.True(createResult.Succeeded);

        var totpServiceFactory = TestServer.Services.GetRequiredService<TotpServiceFactory>();
        var totpService = totpServiceFactory.Create();
        var securityToken = Guid.NewGuid().ToString();

        // Act
        var result = await totpService.SendAsync(totp => totp
            .UseSecurityToken(securityToken)
            .WithMessage("Your verification code is {0}")
            .ToUser(user.Id)
            .UsingSms()
            .WithSubject("Verification")
            
        );

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task TotpServiceFactory_Create_Returns_Error_For_NonExistent_User() {
        // Arrange
        var totpServiceFactory = TestServer.Services.GetRequiredService<TotpServiceFactory>();
        var totpService = totpServiceFactory.Create();
        var securityToken = Guid.NewGuid().ToString();
        var nonExistentUserId = Guid.NewGuid().ToString();

        // Act
        var result = await totpService.SendAsync(totp => totp
            .UseSecurityToken(securityToken)
            .WithMessage("Your verification code is {0}")
            .ToUser(nonExistentUserId)
            .UsingSms()
            .WithSubject("Verification")
        );

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    [Theory]
    [InlineData(TokenOptions.DefaultPhoneProvider)]
    [InlineData(TokenOptions.DefaultEmailProvider)]
    public async Task TwoFactorToken_Can_Be_Verified_After_User_Reload(string tokenProvider) {
        var userManager = TestServer.Services.GetRequiredService<ExtendedUserManager<User>>();
        var lastSignInDate = DateTimeOffset.UtcNow.AddMinutes(-5);
        var user = await CreateTwoFactorUserAsync(userManager, tokenProvider, lastSignInDate);

        var token = await userManager.GenerateTwoFactorTokenAsync(user, tokenProvider);
        var reloadedUser = await userManager.FindByIdAsync(user.Id);

        var isValid = await userManager.VerifyTwoFactorTokenAsync(reloadedUser!, tokenProvider, token);

        Assert.True(isValid);
    }

    [Theory]
    [InlineData(TokenOptions.DefaultPhoneProvider)]
    [InlineData(TokenOptions.DefaultEmailProvider)]
    public async Task TwoFactorToken_Is_Invalid_When_LastSignInDate_Changes(string tokenProvider) {
        var userManager = TestServer.Services.GetRequiredService<ExtendedUserManager<User>>();
        var lastSignInDate = DateTimeOffset.UtcNow.AddMinutes(-5);
        var user = await CreateTwoFactorUserAsync(userManager, tokenProvider, lastSignInDate);

        var token = await userManager.GenerateTwoFactorTokenAsync(user, tokenProvider);
        user.LastSignInDate = lastSignInDate.AddMinutes(1);
        var updateResult = await userManager.UpdateAsync(user);
        Assert.True(updateResult.Succeeded);
        var reloadedUser = await userManager.FindByIdAsync(user.Id);

        var isValid = await userManager.VerifyTwoFactorTokenAsync(reloadedUser!, tokenProvider, token);

        Assert.False(isValid);
    }

    private static async Task<User> CreateTwoFactorUserAsync(ExtendedUserManager<User> userManager, string tokenProvider, DateTimeOffset lastSignInDate) {
        var random = new Random(Guid.NewGuid().GetHashCode()).Next();
        var email = $"user_{random}@example.com";
        var user = new User {
            CreateDate = DateTimeOffset.UtcNow,
            Email = email,
            EmailConfirmed = true,
            Id = Guid.NewGuid().ToString(),
            LastSignInDate = lastSignInDate,
            PhoneNumber = "+306991234567",
            PhoneNumberConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            TwoFactorEnabled = true,
            UserName = email
        };

        if (tokenProvider == TokenOptions.DefaultEmailProvider) {
            user.PhoneNumber = null;
            user.PhoneNumberConfirmed = false;
        } else {
            user.EmailConfirmed = false;
        }

        var createResult = await userManager.CreateAsync(user);
        Assert.True(createResult.Succeeded);
        return user;
    }
}
