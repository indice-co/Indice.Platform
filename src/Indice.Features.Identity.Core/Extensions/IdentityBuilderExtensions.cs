using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.Core.PasswordValidation;
using Indice.Features.Identity.Core.TokenProviders;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions on <see cref="IdentityBuilder"/></summary>
public static class IdentityBuilderExtensions
{
    /// <summary>Registers an instance of <see cref="ExtendedSignInManager{TUser}"/> along with required dependencies.</summary>
    /// <typeparam name="TUser">The type of <see cref="User"/> used by the identity system.</typeparam>
    /// <param name="builder">The type of builder for configuring identity services.</param>
    public static IdentityBuilder AddExtendedSignInManager<TUser>(this IdentityBuilder builder) where TUser : User {
        builder.Services.AddAuthentication().AddCookie(ExtendedIdentityConstants.ExtendedValidationUserIdScheme, options => {
            options.Cookie.Name = ExtendedIdentityConstants.ExtendedValidationUserIdScheme;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        });
        builder.Services.AddAuthentication().AddCookie(ExtendedIdentityConstants.MfaOnboardingScheme, options => {
            options.Cookie.Name = ExtendedIdentityConstants.MfaOnboardingScheme;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        });
        builder.Services.AddTransient<IMfaDeviceIdResolver, MfaDeviceIdResolverHttpContext>();
        builder.Services.TryAddTransient<IAuthenticationMethodProvider, AuthenticationMethodProviderInMemory>();
        builder.AddSignInManager<ExtendedSignInManager<TUser>>();
        builder.Services.TryAddTransient<IUserStateProvider<TUser>, DefaultUserStateProvider<TUser>>();
        return builder;
    }

    /// <summary>Registers an instance of <see cref="ExtendedSignInManager{TUser}"/> along with required dependencies, using <see cref="User"/> class as a user type..</summary>
    /// <param name="builder">The type of builder for configuring identity services.</param>
    public static IdentityBuilder AddExtendedSignInManager(this IdentityBuilder builder) => builder.AddExtendedSignInManager<User>();

    /// <summary>Registers an instance of <see cref="ExtendedUserManager{TUser}"/> along with required dependencies.</summary>
    /// <typeparam name="TUser">The type of <see cref="User"/> used by the identity system.</typeparam>
    /// <param name="builder">The type of builder for configuring identity services.</param>
    public static IdentityBuilder AddExtendedUserManager<TUser>(this IdentityBuilder builder) where TUser : User, new() {
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
    /// Make sure you call this method after using <see cref="Microsoft.AspNetCore.Identity.IdentityBuilderExtensions.AddDefaultTokenProviders(IdentityBuilder)"/>.
    /// </summary>
    /// <param name="builder">Helper functions for configuring identity services.</param>
    /// <param name="configuration"></param>
    /// <param name="configure">Action used to configure the <see cref="TotpOptions"/>.</param>
    /// <returns>The configured <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddExtendedPhoneNumberTokenProvider(this IdentityBuilder builder, IConfiguration configuration, Action<TotpOptions> configure = null) {
        if (configure is not null) {
            builder.Services.PostConfigure(configure);
        }
        builder.Services.AddTotpServiceFactory(configuration, configure);
        builder.AddTokenProvider(TokenOptions.DefaultPhoneProvider, typeof(DeveloperPhoneNumberTokenProvider<>).MakeGenericType(builder.UserType));
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
