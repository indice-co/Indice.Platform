#if NET9_0_OR_GREATER
using Duende.IdentityModel;
#else
using IdentityModel;
#endif
using System.Security.Claims;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Hubs;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.UI;
using Indice.Features.Identity.UI.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Indice.Features.Identity.Tests;

public class SecurityPageTests : IAsyncLifetime
{
    private readonly Mock<ITempDataProvider> _tempDataProvider = new();
    private readonly IdentityUIOptions _uiOptions = new();
    private ServiceProvider? _rootProvider;
    private AsyncServiceScope _scope;
    private HttpContext _httpContext = null!;

    public SecurityPageTests() {
        _tempDataProvider.Setup(x => x.LoadTempData(It.IsAny<HttpContext>())).Returns(new Dictionary<string, object>());
    }

    private ExtendedUserManager<User> UserManager => _scope.ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();

    private async Task<User> SetupAsync(User user, string? password = null, bool withMfaProvider = false, params Claim[] claims) {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["General:ApplicationName"] = "Test"
        }).Build();
        var httpContextAccessor = new TestHttpContextAccessor();
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection()
            .AddSingleton(configuration)
            .AddLogging()
            .AddSingleton<IHttpContextAccessor>(httpContextAccessor);
        services.AddTransient<IUserRequirementProvider<User>, UserRequirementProviderNoOp>();
        services.AddTotpServiceFactory(configuration)
                .AddDefaultPlatformEventService()
                .AddSmsServiceNoop()
                .AddPushNotificationServiceNoop()
                .AddLocalization()
                .AddDistributedMemoryCache()
                .AddDbContext<ExtendedIdentityDbContext<User, Role>>(builder => builder.UseInMemoryDatabase(databaseName))
                .AddIdentity<User, Role>()
                .AddDefaultTokenProviders()
                .AddExtendedUserManager()
                .AddExtendedSignInManager()
                .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                .AddIdentityMessageDescriber<IdentityMessageDescriber>();
        if (withMfaProvider) {
            services.AddScoped<IAuthenticationMethodProvider>(sp => new AuthenticationMethodProviderInMemory(
                Enumerable.Empty<IHubContext<MultiFactorAuthenticationHub>>(),
                configuration,
                sp.GetRequiredService<ExtendedUserManager<User>>(),
                [
                    new SmsAuthenticationMethod(new IdentityMessageDescriber()),
                    new EmailAuthenticationMethod(new IdentityMessageDescriber())
                ],
                [
                    new AuthenticationMethodConfiguration { MethodType = typeof(SmsAuthenticationMethod) },
                    new AuthenticationMethodConfiguration { MethodType = typeof(EmailAuthenticationMethod) }
                ]
            ));
        }
        _rootProvider = services.BuildServiceProvider();
        _scope = _rootProvider.CreateAsyncScope();
        _httpContext = new DefaultHttpContext { RequestServices = _scope.ServiceProvider };
        _httpContext.Request.Scheme = "https";
        _httpContext.Request.Host = new HostString("localhost");
        httpContextAccessor.HttpContext = _httpContext;
        var result = password is null ? await UserManager.CreateAsync(user) : await UserManager.CreateAsync(user, password);
        Assert.True(result.Succeeded);
        if (claims.Length > 0) {
            Assert.True((await UserManager.AddClaimsAsync(user, claims)).Succeeded);
        }
        var signInManager = _scope.ServiceProvider.GetRequiredService<ExtendedSignInManager<User>>();
        _httpContext.User = await signInManager.CreateUserPrincipalAsync(user);
        return user;
    }

    private TestSecurityModel CreatePage() {
        var actionContext = new ActionContext(_httpContext, new RouteData(), new CompiledPageActionDescriptor(), new ModelStateDictionary());
        return new TestSecurityModel(UserManager, Options.Create(_uiOptions)) {
            PageContext = new PageContext(actionContext),
            TempData = new TempDataDictionary(_httpContext, _tempDataProvider.Object)
        };
    }

    [Fact]
    public async Task OnGet_Populates_Summary_And_Sign_In_Methods() {
        await SetupAsync(new User {
            UserName = "someone",
            Email = "someone@somewhere.com",
            EmailConfirmed = true,
            PhoneNumber = "+306900000000",
            PhoneNumberConfirmed = false
        }, claims: [new Claim(JwtClaimTypes.GivenName, "John"), new Claim(JwtClaimTypes.FamilyName, "Doe")]);
        var page = CreatePage();

        var result = await page.OnGetAsync();

        Assert.IsType<PageResult>(result);
        var view = page.View;
        Assert.Equal("John Doe", view.DisplayName);
        Assert.False(view.HasPassword);
        Assert.Equal("someone@somewhere.com", view.Email);
        Assert.True(view.EmailConfirmed);
        Assert.True(view.CanEditEmail);
        Assert.Equal("+306900000000", view.PhoneNumber);
        Assert.False(view.PhoneNumberConfirmed);
        Assert.True(view.CanEditPhone);
        Assert.False(view.TwoFactorEnabled);
        Assert.Null(view.TwoFactorMethodName);
        Assert.False(view.CanManageMfa);
    }

    [Fact]
    public async Task OnGet_Falls_Back_To_UserName_And_Detects_Password() {
        await SetupAsync(new User { UserName = "someone", Email = "someone@somewhere.com" }, password: "P@ssw0rd!Strong123");
        var page = CreatePage();

        await page.OnGetAsync();

        Assert.Equal("someone", page.View.DisplayName);
        Assert.True(page.View.HasPassword);
        Assert.False(page.View.EmailConfirmed);
        Assert.Null(page.View.PhoneNumber);
        Assert.False(page.View.PhoneNumberConfirmed);
    }

    [Fact]
    public async Task OnGet_Respects_Disabled_Email_And_Phone_Edit() {
        await SetupAsync(new User { UserName = "someone", Email = "someone@somewhere.com" });
        _uiOptions.DisableEmailEdit = true;
        _uiOptions.DisablePhoneEdit = true;
        var page = CreatePage();

        await page.OnGetAsync();

        Assert.False(page.View.CanEditEmail);
        Assert.False(page.View.CanEditPhone);
    }

    [Fact]
    public async Task OnGet_Allows_Mfa_Management_When_Methods_Are_Available() {
        await SetupAsync(new User {
            UserName = "someone",
            Email = "someone@somewhere.com",
            EmailConfirmed = true,
            TwoFactorEnabled = true
        }, withMfaProvider: true);
        var page = CreatePage();

        await page.OnGetAsync();

        Assert.True(page.View.TwoFactorEnabled);
        Assert.True(page.View.CanManageMfa);
        Assert.False(string.IsNullOrWhiteSpace(page.View.TwoFactorMethodName));
    }

    [Fact]
    public async Task OnGet_Hides_Mfa_Management_When_Disabled_By_Option() {
        await SetupAsync(new User { UserName = "someone", Email = "someone@somewhere.com", EmailConfirmed = true }, withMfaProvider: true);
        _uiOptions.DisableMfaEdit = true;
        var page = CreatePage();

        await page.OnGetAsync();

        Assert.False(page.View.CanManageMfa);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() {
        await _scope.DisposeAsync();
        if (_rootProvider is not null) {
            await _rootProvider.DisposeAsync();
        }
    }

    private sealed class TestSecurityModel(ExtendedUserManager<User> userManager, IOptions<IdentityUIOptions> identityUiOptions) : BaseSecurityModel(userManager, identityUiOptions);

    private sealed class TestHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
