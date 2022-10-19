using System;
using System.Security;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity
{
    /// <summary>A factory service that contains methods to create various TOTP services, based on <see cref="TotpServiceBase"/>.</summary>
    public class TotpServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>Creates a new instance of <see cref="TotpServiceFactory"/>.</summary>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public TotpServiceFactory(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>Creates a new instance of <see cref="UserTotpService{TUser}"/>.</summary>
        /// <typeparam name="TUser">The type of user entity.</typeparam>
        public UserTotpService<TUser> Create<TUser>() where TUser : User {
            var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<TUser>>();
            var options = _serviceProvider.GetRequiredService<IOptions<TotpOptions>>();
            if (options.Value.EnableDeveloperTotp) {
                var developerTotpServiceLocalizer = _serviceProvider.GetRequiredService<IStringLocalizer<DeveloperTotpService<TUser>>>();
                return new DeveloperTotpService<TUser>(userManager, developerTotpServiceLocalizer, _serviceProvider);
            }
            var userTotpServiceLocalizer = _serviceProvider.GetRequiredService<IStringLocalizer<UserTotpService<TUser>>>();
            return new UserTotpService<TUser>(userManager, userTotpServiceLocalizer, _serviceProvider);
        }

        /// <summary>Creates a new instance of <see cref="SecurityTokenTotpService"/>.</summary>
        public SecurityTokenTotpService Create() {
            var rfc6238AuthenticationService = _serviceProvider.GetRequiredService<Rfc6238AuthenticationService>();
            var localizer = _serviceProvider.GetRequiredService<IStringLocalizer<SecurityTokenTotpService>>();
            return new SecurityTokenTotpService(_serviceProvider, rfc6238AuthenticationService, localizer);
        }
    }
}
