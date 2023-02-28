using System.IdentityModel.Tokens.Jwt;
using Indice.AspNetCore.Identity;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.PasswordValidation;
using Indice.Identity.Security;
using Indice.Identity.Services;
using Microsoft.AspNetCore.Identity;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Contains methods to configure application's identity system.</summary>
public static class IdentityConfig
{
    /// <summary>Configures various aspects of the ASP.NET Core Identity system.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static IdentityBuilder AddIdentityConfig(this IServiceCollection services, IConfiguration configuration) {
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
        services.Configure<IdentityOptions>(configuration.GetSection(nameof(IdentityOptions)));
        services.AddScopeTransformation();
        services.AddExternalProviderClaimsTransformation();
        return services.AddIdentity<DbUser, DbRole>()
                       .AddErrorDescriber<LocalizedIdentityErrorDescriber>()
                       .AddIdentityMessageDescriber<LocalizedIdentityMessageDescriber>()
                       .AddClaimsPrincipalFactory<MicrosoftGraphUserClaimsPrincipalFactory>()
                       .AddEntityFrameworkStores<ExtendedIdentityDbContext<DbUser, DbRole>>()
                       .AddUserManager<ExtendedUserManager<DbUser>>()
                       .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<DbUser, DbRole>, DbUser, DbRole>>()
                       .AddExtendedSignInManager()
                       .AddDefaultPasswordValidators()
                       .AddPasswordValidator<AllowedCharactersPasswordValidator>()
                       .AddDefaultTokenProviders()
                       .AddExtendedPhoneNumberTokenProvider();
    }
}
