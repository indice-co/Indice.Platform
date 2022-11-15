using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Events;
using Indice.Services;
using Microsoft.Extensions.Logging;

namespace Indice.Identity.Services
{
    /// <summary>
    /// Handler for <see cref="EmailConfirmedEvent"/> raised by IdentityServer API.
    /// </summary>
    public class UserEmailConfirmedEventHandler : IPlatformEventHandler<EmailConfirmedEvent>
    {
        private readonly ILogger<UserEmailConfirmedEventHandler> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="UserEmailConfirmedEventHandler"/>.
        /// </summary>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public UserEmailConfirmedEventHandler(ILogger<UserEmailConfirmedEventHandler> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task Handle(EmailConfirmedEvent @event) {
            _logger.LogDebug($"User confirmed email: {@event}");
            return Task.CompletedTask;
        }
    }
}
