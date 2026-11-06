using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Tests;

public class EmailConfirmationOtpTests : IAsyncLifetime
{
    public EmailConfirmationOtpTests() {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var services = new ServiceCollection()
            .AddSingleton(configuration)
            .AddLogging();
        services.AddTransient<IUserRequirementProvider<User>, UserRequirementProviderNoOp>();
        services.AddTotpServiceFactory(configuration)
                .AddDefaultPlatformEventService()
                .AddSmsServiceNoop()
                .AddPushNotificationServiceNoop()
                .AddLocalization()
                .AddDistributedMemoryCache()
                .AddDbContext<ExtendedIdentityDbContext<User, Role>>(builder => builder.UseInMemoryDatabase(Guid.NewGuid().ToString()))
                .AddIdentity<User, Role>()
                .AddExtendedUserManager()
                .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                .AddDefaultTokenProviders()
                .AddErrorDescriber<ExtendedIdentityErrorDescriber>()
                .AddIdentityMessageDescriber<IdentityMessageDescriber>();
        ServiceProvider = services.BuildServiceProvider();
    }

    public ServiceProvider ServiceProvider { get; }

    private async Task<(ExtendedUserManager<User> UserManager, User User)> CreateUserAsync() {
        var userManager = ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var user = new User {
            Email = "someone@somewhere.com",
            UserName = "someone@somewhere.com",
            EmailConfirmed = false
        };
        var result = await userManager.CreateAsync(user);
        Assert.True(result.Succeeded);
        return (userManager, user);
    }

    [Fact]
    public async Task ConfirmEmailWithOtp_Confirms_Email_When_Code_Is_Valid() {
        var (userManager, user) = await CreateUserAsync();
        var code = await userManager.GenerateEmailConfirmationOtpAsync(user);

        var result = await userManager.ConfirmEmailWithOtpAsync(user, code);

        Assert.True(result.Succeeded);
        Assert.True(await userManager.IsEmailConfirmedAsync(user));
    }

    [Fact]
    public async Task ConfirmEmailWithOtp_Fails_When_Code_Is_Invalid() {
        var (userManager, user) = await CreateUserAsync();
        var code = await userManager.GenerateEmailConfirmationOtpAsync(user);
        var wrongCode = code == "000000" ? "111111" : "000000";

        var result = await userManager.ConfirmEmailWithOtpAsync(user, wrongCode);

        Assert.False(result.Succeeded);
        Assert.False(await userManager.IsEmailConfirmedAsync(user));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ConfirmEmailWithOtp_Fails_When_Code_Is_Empty(string code) {
        var (userManager, user) = await CreateUserAsync();

        var result = await userManager.ConfirmEmailWithOtpAsync(user, code);

        Assert.False(result.Succeeded);
        Assert.False(await userManager.IsEmailConfirmedAsync(user));
    }

    [Fact]
    public async Task ConfirmEmailWithOtp_Rejects_TwoFactor_Code_With_Different_Purpose() {
        var (userManager, user) = await CreateUserAsync();
        var mfaCode = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

        var result = await userManager.ConfirmEmailWithOtpAsync(user, mfaCode);

        Assert.False(result.Succeeded);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => ServiceProvider.DisposeAsync();
}
