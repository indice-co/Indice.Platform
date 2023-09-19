using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.Core.PasswordValidation;
using Indice.Features.Identity.Core.TokenProviders;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SixLabors.ImageSharp;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions on <see cref="IdentityBuilder"/>.</summary>
public static class IdentityBuilderExtensions
{
    /// <summary>Registers an instance of <see cref="ExtendedSignInManager{TUser}"/> along with required dependencies.</summary>
    /// <typeparam name="TUser">The type of <see cref="User"/> used by the identity system.</typeparam>
    /// <param name="builder">The type of builder for configuring identity services.</param>
    public static IdentityBuilder AddExtendedSignInManager<TUser>(this IdentityBuilder builder) where TUser : User {
        static Action<CookieAuthenticationOptions> AuthCookie(string cookieName) => options => {
            options.Cookie.Name = cookieName;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            options.LoginPath = new PathString("/login");
            options.LogoutPath = new PathString("/logout");
            options.AccessDeniedPath = new PathString("/403");
        };
        builder.Services
               .AddAuthentication()
               .AddCookie(ExtendedIdentityConstants.ExtendedValidationUserIdScheme, AuthCookie(ExtendedIdentityConstants.ExtendedValidationUserIdScheme))
               .AddCookie(ExtendedIdentityConstants.MfaOnboardingScheme, AuthCookie(ExtendedIdentityConstants.MfaOnboardingScheme));
        builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorUserIdScheme, options => {
            AuthCookie(IdentityConstants.TwoFactorUserIdScheme)(options);
            options.LoginPath = new PathString("/login-with-2fa");
        });
        builder.Services.AddTransient<IDeviceIdResolver, DeviceIdResolverHttpContext>();
        builder.Services.TryAddTransient<IAuthenticationMethodProvider, AuthenticationMethodProviderInMemory>();
        builder.AddSignInManager<ExtendedSignInManager<TUser>>();
        builder.Services.TryAddScoped<IUserStateProvider<TUser>, DefaultUserStateProvider<TUser>>();
        return builder;
    }

    /// <summary>Registers an instance of <see cref="ExtendedSignInManager{TUser}"/> along with required dependencies, using <see cref="User"/> class as a user type..</summary>
    /// <param name="builder">The type of builder for configuring identity services.</param>
    public static IdentityBuilder AddExtendedSignInManager(this IdentityBuilder builder) => builder.AddExtendedSignInManager<User>();

    /// <summary>Registers an instance of <see cref="ExtendedUserManager{TUser}"/> along with required dependencies.</summary>
    /// <typeparam name="TUser">The type of <see cref="User"/> used by the identity system.</typeparam>
    /// <param name="builder">The type of builder for configuring identity services.</param>
    public static IdentityBuilder AddExtendedUserManager<TUser>(this IdentityBuilder builder) where TUser : User, new() {
        builder.Services.AddPlatformEventHandler<UserBlockedEvent, UserBlockedEventHandler>();
        builder.AddEntityFrameworkStores<ExtendedIdentityDbContext<TUser, Role>>()
               .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<TUser, Role>, TUser, Role>>()
               .AddUserManager<ExtendedUserManager<TUser>>();
        return builder;
    }

    /// <summary>Registers an instance of <see cref="ExtendedUserManager{TUser}"/> along with required dependencies.</summary>
    /// <param name="builder">The type of builder for configuring identity services.</param>
    public static IdentityBuilder AddExtendedUserManager(this IdentityBuilder builder) => builder.AddExtendedUserManager<User>();

    /// <summary>
    /// Adds the <see cref="ExtendedPhoneNumberTokenProvider{TUser}"/> as the default phone provider.
    /// Make sure you call this method after using <see cref="AspNetCore.Identity.IdentityBuilderExtensions.AddDefaultTokenProviders(IdentityBuilder)"/>.
    /// </summary>
    /// <param name="builder">Helper functions for configuring identity services.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Action used to configure the <see cref="TotpOptions"/>.</param>
    /// <returns>The configured <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddExtendedPhoneNumberTokenProvider(this IdentityBuilder builder, IConfiguration configuration, Action<PhoneNumberTokenProviderTotpOptions> configure = null) {
        var totpSection = configuration.GetSection(TotpOptions.Name);
        var totpPhoneSection = configuration.GetSection(PhoneNumberTokenProviderTotpOptions.Name);
        var totpOptions = new PhoneNumberTokenProviderTotpOptions {
            CodeDuration = totpPhoneSection.GetValue<int?>(nameof(PhoneNumberTokenProviderTotpOptions.CodeDuration)) ??
                           totpSection.GetValue<int?>(nameof(PhoneNumberTokenProviderTotpOptions.CodeDuration)) ??
                           TotpOptionsBase.DefaultCodeDuration,
            CodeLength = totpPhoneSection.GetValue<int?>(nameof(PhoneNumberTokenProviderTotpOptions.CodeLength)) ??
                         totpSection.GetValue<int?>(nameof(PhoneNumberTokenProviderTotpOptions.CodeLength)) ??
                         TotpOptionsBase.DefaultCodeLength,
            EnableDeveloperTotp = totpPhoneSection.GetValue<bool?>(nameof(PhoneNumberTokenProviderTotpOptions.EnableDeveloperTotp)) ??
                                  totpSection.GetValue<bool?>(nameof(PhoneNumberTokenProviderTotpOptions.EnableDeveloperTotp)) ??
                                  false
        };
        configure?.Invoke(totpOptions);
        builder.Services.Configure<PhoneNumberTokenProviderTotpOptions>(options => {
            options.CodeLength = totpOptions.CodeLength;
            options.CodeDuration = totpOptions.CodeDuration;
            options.EnableDeveloperTotp = totpOptions.EnableDeveloperTotp;
        });
        builder.AddTokenProvider(TokenOptions.DefaultPhoneProvider, typeof(DeveloperPhoneNumberTokenProvider<>).MakeGenericType(builder.UserType));
        return builder;
    }

    /// <summary>
    /// Adds the <see cref="ExtendedEmailTokenProvider{TUser}"/> as the default email provider.
    /// Make sure you call this method after using <see cref="AspNetCore.Identity.IdentityBuilderExtensions.AddDefaultTokenProviders(IdentityBuilder)"/>.
    /// </summary>
    /// <param name="builder">Helper functions for configuring identity services.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Action used to configure the <see cref="TotpOptions"/>.</param>
    /// <returns>The configured <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddExtendedEmailTokenProvider(this IdentityBuilder builder, IConfiguration configuration, Action<EmailTokenProviderTotpOptions> configure = null) {
        var totpSection = configuration.GetSection(TotpOptions.Name);
        var totpEmailSection = configuration.GetSection(EmailTokenProviderTotpOptions.Name);
        var totpOptions = new EmailTokenProviderTotpOptions {
            CodeDuration = totpEmailSection.GetValue<int?>(nameof(EmailTokenProviderTotpOptions.CodeDuration)) ??
                           totpSection.GetValue<int?>(nameof(EmailTokenProviderTotpOptions.CodeDuration)) ??
                           TotpOptionsBase.DefaultCodeDuration,
            CodeLength = totpEmailSection.GetValue<int?>(nameof(EmailTokenProviderTotpOptions.CodeLength)) ??
                         totpSection.GetValue<int?>(nameof(EmailTokenProviderTotpOptions.CodeLength)) ??
                         TotpOptionsBase.DefaultCodeLength,
            EnableDeveloperTotp = totpEmailSection.GetValue<bool?>(nameof(EmailTokenProviderTotpOptions.EnableDeveloperTotp)) ??
                                  totpSection.GetValue<bool?>(nameof(EmailTokenProviderTotpOptions.EnableDeveloperTotp)) ??
                                  false
        };
        configure?.Invoke(totpOptions);
        builder.Services.Configure<EmailTokenProviderTotpOptions>(options => {
            options.CodeLength = totpOptions.CodeLength;
            options.CodeDuration = totpOptions.CodeDuration;
            options.EnableDeveloperTotp = totpOptions.EnableDeveloperTotp;
        });
        builder.AddTokenProvider(TokenOptions.DefaultEmailProvider, typeof(ExtendedEmailTokenProvider<>).MakeGenericType(builder.UserType));
        return builder;
    }

    /// <summary>Configures options for <see cref="ExtendedPhoneNumberTokenProvider{TUser}"/>.</summary>
    /// <param name="builder">Helper functions for configuring identity services.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Action used to configure the <see cref="TotpOptions"/>.</param>
    /// <returns>The configured <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder ConfigureExtendedPhoneNumberTokenProvider(this IdentityBuilder builder, IConfiguration configuration, Action<PhoneNumberTokenProviderTotpOptions> configure = null) {
        var totpSection = configuration.GetSection(PhoneNumberTokenProviderTotpOptions.Name);
        var totpOptions = new PhoneNumberTokenProviderTotpOptions {
            CodeDuration = totpSection.GetValue<int?>(nameof(PhoneNumberTokenProviderTotpOptions.CodeDuration)) ?? TotpOptionsBase.DefaultCodeDuration,
            CodeLength = totpSection.GetValue<int?>(nameof(PhoneNumberTokenProviderTotpOptions.CodeLength)) ?? TotpOptionsBase.DefaultCodeLength
        };
        configure?.Invoke(totpOptions);
        builder.Services.PostConfigure<PhoneNumberTokenProviderTotpOptions>(options => {
            options.CodeLength = totpOptions.CodeLength;
            options.CodeDuration = totpOptions.CodeDuration;
        });
        return builder;
    }

    /// <summary>Configures options for <see cref="ExtendedEmailTokenProvider{TUser}"/>.</summary>
    /// <param name="builder">Helper functions for configuring identity services.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="configure">Action used to configure the <see cref="TotpOptions"/>.</param>
    /// <returns>The configured <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder ConfigureExtendedEmailTokenProvider(this IdentityBuilder builder, IConfiguration configuration, Action<EmailTokenProviderTotpOptions> configure = null) {
        var totpSection = configuration.GetSection(EmailTokenProviderTotpOptions.Name);
        var totpOptions = new EmailTokenProviderTotpOptions {
            CodeDuration = totpSection.GetValue<int?>(nameof(EmailTokenProviderTotpOptions.CodeDuration)) ?? TotpOptionsBase.DefaultCodeDuration,
            CodeLength = totpSection.GetValue<int?>(nameof(EmailTokenProviderTotpOptions.CodeLength)) ?? TotpOptionsBase.DefaultCodeLength
        };
        configure?.Invoke(totpOptions);
        builder.Services.PostConfigure<EmailTokenProviderTotpOptions>(options => {
            options.CodeLength = totpOptions.CodeLength;
            options.CodeDuration = totpOptions.CodeDuration;
        });
        return builder;
    }

    /// <summary>Registers the <see cref="AuthenticationMethodProviderInMemory"/> which is an in-memory static provider for <see cref="IAuthenticationMethodProvider"/>.</summary>
    /// <param name="builder">Helper functions for configuring identity services.</param>
    /// <param name="authenticationMethod">An authentication method to apply in the identity system.</param>
    /// <param name="otherAuthenticationMethods">The authentication methods to apply in the identity system.</param>
    /// <returns>The configured <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddAuthenticationMethodProvider(this IdentityBuilder builder, AuthenticationMethod authenticationMethod, params AuthenticationMethod[] otherAuthenticationMethods) {
        var allMethods = (otherAuthenticationMethods ?? Array.Empty<AuthenticationMethod>()).Prepend(authenticationMethod);
        foreach (var method in allMethods) {
            builder.Services.AddSingleton(method);
        }
        builder.Services.AddTransient<IAuthenticationMethodProvider, AuthenticationMethodProviderInMemory>();
        return builder;
    }

    /// <summary>Registers an implementation of <see cref="IAuthenticationMethodProvider"/>.</summary>
    /// <typeparam name="TAuthenticationMethodProvider"></typeparam>
    /// <param name="builder">Helper functions for configuring identity services.</param>
    /// <returns>The configured <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddAuthenticationMethodProvider<TAuthenticationMethodProvider>(this IdentityBuilder builder) where TAuthenticationMethodProvider : IAuthenticationMethodProvider {
        builder.Services.AddTransient(typeof(IAuthenticationMethodProvider), typeof(TAuthenticationMethodProvider));
        return builder;
    }

    /// <summary>
    /// Registers <see cref="NonCommonPasswordValidator{T}"/> as a password validator along with two <see cref="IPasswordBlacklistProvider"/>, the <see cref="DefaultPasswordBlacklistProvider"/>
    /// and <see cref="ConfigPasswordBlacklistProvider"/>.
    /// </summary>
    /// <typeparam name="TUser">The type of the <see cref="IdentityUser"/>.</typeparam>
    /// <param name="builder">Helper functions for configuring identity services.</param>
    /// <returns>The <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddNonCommonPasswordValidator<TUser>(this IdentityBuilder builder) where TUser : User {
        builder.Services.AddSingleton<IPasswordBlacklistProvider, DefaultPasswordBlacklistProvider>();
        builder.Services.AddSingleton<IPasswordBlacklistProvider, ConfigPasswordBlacklistProvider>();
        builder.AddPasswordValidator<NonCommonPasswordValidator<TUser>>();
        return builder;
    }

    /// <summary>
    /// Registers <see cref="NonCommonPasswordValidator"/> as a password validator along with two <see cref="IPasswordBlacklistProvider"/>, the <see cref="DefaultPasswordBlacklistProvider"/>
    /// and <see cref="ConfigPasswordBlacklistProvider"/>, using <see cref="User"/> class as a user type.
    /// </summary>
    /// <param name="builder">Helper functions for configuring identity services.</param>
    /// <returns>The <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddNonCommonPasswordValidator(this IdentityBuilder builder) => builder.AddNonCommonPasswordValidator<User>();

    /// <summary>Registers the recommended password validators: <see cref="NonCommonPasswordValidator"/>, <see cref="UnicodeCharactersPasswordValidator"/>, <see cref="PreviousPasswordAwareValidator"/> and <see cref="UserNameAsPasswordValidator"/>.</summary>
    /// <param name="builder">Helper functions for configuring identity services.</param>
    /// <returns>The <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddDefaultPasswordValidators(this IdentityBuilder builder) {
        builder.AddNonCommonPasswordValidator();
        builder.AddPasswordValidator<UnicodeCharactersPasswordValidator>();
        builder.AddPasswordValidator<PreviousPasswordAwareValidator<ExtendedIdentityDbContext<User, Role>, User, Role>>();
        builder.AddPasswordValidator<UserNameAsPasswordValidator>();
        return builder;
    }

    /// <summary>Adds an overridden implementation of <see cref="IdentityMessageDescriber"/>.</summary>
    /// <param name="builder">Helper functions for configuring identity services.</param>
    /// <remarks>The <see cref="LocalizedIdentityMessageDescriber"/> is registered.</remarks>
    public static IdentityBuilder AddIdentityMessageDescriber(this IdentityBuilder builder) => builder.AddIdentityMessageDescriber<LocalizedIdentityMessageDescriber>();

    /// <summary>Adds an overridden implementation of <see cref="IdentityMessageDescriber"/>.</summary>
    /// <typeparam name="TDescriber">The type of message describer.</typeparam>
    /// <param name="builder">Helper functions for configuring identity services.</param>
    public static IdentityBuilder AddIdentityMessageDescriber<TDescriber>(this IdentityBuilder builder) where TDescriber : IdentityMessageDescriber {
        builder.Services.AddScoped<IdentityMessageDescriber, TDescriber>();
        return builder;
    }

    /// <summary>Adds an <see cref="IdentityErrorDescriber"/>.</summary>
    /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
    /// <remarks>The <see cref="LocalizedIdentityErrorDescriber"/> is registered.</remarks>
    public static IdentityBuilder AddErrorDescriber(this IdentityBuilder builder) => builder.AddErrorDescriber<LocalizedIdentityErrorDescriber>();
}
