using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Indice.Features.Identity.Core.Models;
using Microsoft.AspNetCore.SignalR;
using Indice.Features.Identity.Core.Hubs;
using Indice.Services;

namespace Indice.Features.Identity.Tests;

public class AuthenticationMethodProviderInMemoryTests : IAsyncLifetime
{
    public AuthenticationMethodProviderInMemoryTests() {
        var inMemorySettings = new Dictionary<string, string?> {
            ["IdentityOptions:SignIn:RequirePostSignInConfirmedEmail"] = "true",
            ["IdentityOptions:SignIn:RequirePostSignInConfirmedPhoneNumber"] = "true",
            ["IdentityOptions:SignIn:Mfa:Policy"] = "Default",
            ["IdentityOptions:SignIn:Mfa:AllowDowngradeAuthenticationMethod"] = "true",
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
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
                .AddExtendedSignInManager()
                .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                .AddExtendedPhoneNumberTokenProvider(configuration)
                .AddIdentityMessageDescriber<IdentityMessageDescriber>();
        ServiceProvider = services.BuildServiceProvider();
    }
    public ServiceProvider ServiceProvider { get; }

    [Fact]
    public async Task GetAllMethodsForUser_Allows_Only_Configured_Sms_Methods() {
        var authenticationMethodProvider = new AuthenticationMethodProviderInMemory(
            Enumerable.Empty<IHubContext<MultiFactorAuthenticationHub>>(),
            ServiceProvider.GetRequiredService<IConfiguration>(),
            ServiceProvider.GetRequiredService<ExtendedUserManager<User>>(),
            [
               new TrustedDeviceAuthenticationMethod(new IdentityMessageDescriber()),
               new SmsAuthenticationMethod(new IdentityMessageDescriber()),
               new EmailAuthenticationMethod(new IdentityMessageDescriber())
            ],
            [
               new AuthenticationMethodConfiguration() { MethodType = typeof(TrustedDeviceAuthenticationMethod), SupportsMfa = true },
               new AuthenticationMethodConfiguration() { MethodType = typeof(SmsAuthenticationMethod), SupportsMfa = true },
               new AuthenticationMethodConfiguration() { MethodType = typeof(EmailAuthenticationMethod), SupportsMfa = false }
            ]
        );
        var userManager = ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var user = new User {
            Email = "someone@somewhere.com",
            UserName = "someone@somewhere.com",
            PhoneNumber = "+306900000000",
            PhoneNumberConfirmed = true,
        };
        await userManager.CreateAsync(user);

        var methods = await authenticationMethodProvider.GetAllMethodsForUserAsync(user);
        Assert.NotEmpty(methods);
    }

    [Fact]
    public async Task GetAllMethodsForUser_Allows_Only_Configured_TrustedDevice_Methods() {
        var authenticationMethodProvider = new AuthenticationMethodProviderInMemory(
            Enumerable.Empty<IHubContext<MultiFactorAuthenticationHub>>(),
            ServiceProvider.GetRequiredService<IConfiguration>(),
            ServiceProvider.GetRequiredService<ExtendedUserManager<User>>(),
            [
               new TrustedDeviceAuthenticationMethod(new IdentityMessageDescriber()),
               new SmsAuthenticationMethod(new IdentityMessageDescriber()),
               new EmailAuthenticationMethod(new IdentityMessageDescriber())
            ],
            [
               new AuthenticationMethodConfiguration() { MethodType = typeof(TrustedDeviceAuthenticationMethod), SupportsMfa = true },
               new AuthenticationMethodConfiguration() { MethodType = typeof(SmsAuthenticationMethod), SupportsMfa = true },
               new AuthenticationMethodConfiguration() { MethodType = typeof(EmailAuthenticationMethod), SupportsMfa = true }
            ]
        );
        var userManager = ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var user = new User {
            Email = "someone@somewhere.com",
            UserName = "someone@somewhere.com",
            PhoneNumber = "+306900000000",
            PhoneNumberConfirmed = false,
        };
        await userManager.CreateAsync(user);
        await userManager.CreateDeviceAsync(user, new UserDevice {
            DeviceId = Guid.NewGuid().ToString(),
            Name = "Test device",
            ClientType = DeviceClientType.Native,
            IsTrusted = true,
        }, TestContext.Current.CancellationToken);

        var methods = await authenticationMethodProvider.GetAllMethodsForUserAsync(user);
        Assert.Single(methods);
        var defaultMethod = await authenticationMethodProvider.FindMethodForUserOrDefaultAsync(user);
        Assert.Equal(methods[0].Type, defaultMethod?.Type);
    }

    [Fact]
    public async Task GetAllMethodsForUser_Allows_Only_Configured_Sms_TrustedDevice_Methods() {
        var authenticationMethodProvider = new AuthenticationMethodProviderInMemory(
            Enumerable.Empty<IHubContext<MultiFactorAuthenticationHub>>(),
            ServiceProvider.GetRequiredService<IConfiguration>(),
            ServiceProvider.GetRequiredService<ExtendedUserManager<User>>(),
            [
               new TrustedDeviceAuthenticationMethod(new IdentityMessageDescriber()),
               new SmsAuthenticationMethod(new IdentityMessageDescriber()),
               new EmailAuthenticationMethod(new IdentityMessageDescriber())
            ],
            [
               new AuthenticationMethodConfiguration() { MethodType = typeof(TrustedDeviceAuthenticationMethod)},
               new AuthenticationMethodConfiguration() { MethodType = typeof(SmsAuthenticationMethod) },
               new AuthenticationMethodConfiguration() { MethodType = typeof(EmailAuthenticationMethod)}
            ]
        );
        var userManager = ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var user = new User {
            Email = "someone@somewhere.com",
            UserName = "someone@somewhere.com",
            PhoneNumber = "+306900000000",
            PhoneNumberConfirmed = true,
            EmailConfirmed = false,
        };
        await userManager.CreateAsync(user);
        await userManager.CreateDeviceAsync(user, new UserDevice {
            DeviceId = Guid.NewGuid().ToString(),
            Name = "Test device",
            ClientType = DeviceClientType.Native,
            IsTrusted = true,
        }, TestContext.Current.CancellationToken);

        var methods = await authenticationMethodProvider.GetAllMethodsForUserAsync(user);
        Assert.Equal(2, methods.Length);
        var defaultMethod = await authenticationMethodProvider.FindMethodForUserOrDefaultAsync(user);
        Assert.Equal(methods[0].Type, defaultMethod?.Type);
    }

    [Fact]
    public async Task GetAllMethodsForUser_Allows_Only_Configured_Viber_Method() {
        var authenticationMethodProvider = new AuthenticationMethodProviderInMemory(
            Enumerable.Empty<IHubContext<MultiFactorAuthenticationHub>>(),
            ServiceProvider.GetRequiredService<IConfiguration>(),
            ServiceProvider.GetRequiredService<ExtendedUserManager<User>>(),
            [
               new TrustedDeviceAuthenticationMethod(new IdentityMessageDescriber()),
               new SmsAuthenticationMethod(new IdentityMessageDescriber()),
               new ViberAuthenticationMethod(new IdentityMessageDescriber()),
               new EmailAuthenticationMethod(new IdentityMessageDescriber())
            ],
            [
               new AuthenticationMethodConfiguration() { MethodType = typeof(TrustedDeviceAuthenticationMethod)},
               new AuthenticationMethodConfiguration() { MethodType = typeof(SmsAuthenticationMethod) },
               new AuthenticationMethodConfiguration() { MethodType = typeof(ViberAuthenticationMethod) },
               new AuthenticationMethodConfiguration() { MethodType = typeof(EmailAuthenticationMethod)}
            ]
        );
        var userManager = ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var user = new User {
            Email = "someone@somewhere.com",
            UserName = "someone@somewhere.com",
            PhoneNumber = "+306900000000",
            PhoneNumberConfirmed = true,
            EmailConfirmed = true,
        };
        await userManager.CreateAsync(user);
        await userManager.CreateDeviceAsync(user, new UserDevice {
            DeviceId = Guid.NewGuid().ToString(),
            Name = "Test device",
            ClientType = DeviceClientType.Native,
            IsTrusted = true,
        }, TestContext.Current.CancellationToken);

        var methods = await authenticationMethodProvider.GetAllMethodsForUserAsync(user);
        Assert.Equal(4, methods.Length);
        var selectedMethod = await authenticationMethodProvider.FindMethodForUserOrDefaultAsync(user, "Viber");
        Assert.NotEqual(methods[0].Type, selectedMethod?.Type);
        Assert.Equal(TotpDeliveryChannel.Viber, selectedMethod?.GetDeliveryChannel());
    }

    public ValueTask InitializeAsync() {
        return ValueTask.CompletedTask;
    }
    public async ValueTask DisposeAsync() {
        await ServiceProvider.DisposeAsync();
    }
}
