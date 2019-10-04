using System;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Indice.Identity.Security
{
    /// <summary>
    /// Class that helps record the issuance of a token to a user.
    /// </summary>
    public class TokenIssuanceEventSink : IEventSink
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TokenIssuanceEventSink> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="TokenIssuanceEventSink"/>.
        /// </summary>
        /// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public TokenIssuanceEventSink(IHttpContextAccessor httpContextAccessor, ILogger<TokenIssuanceEventSink> logger) {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Persists an event raised by IdentityServer.
        /// </summary>
        /// <param name="event">Information on the raised event.</param>
        public Task PersistAsync(Event @event) {
            if (@event.Id.Equals(EventIds.TokenIssuedSuccess)) {
                var user = _httpContextAccessor.HttpContext.User;
                var userId = user.FindFirst(JwtClaimTypes.Subject);
                var userEmail = user.FindFirst(JwtClaimTypes.Email);
                _logger.LogInformation($"User with Id: '{userId}' and email '{userEmail}' was issued a token.");
            }
            return Task.CompletedTask;
        }
    }
}
