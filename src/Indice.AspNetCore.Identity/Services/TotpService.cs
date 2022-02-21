using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity
{
    /// <summary>
    /// Used to generate, send and verify time based one time passwords.
    /// </summary>
    public class TotpService : ITotpService
    {
        private const int CacheExpirationInSeconds = 30;
        private readonly UserManager<User> _userManager;
        private readonly ISmsServiceFactory _smsServiceFactory;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IDistributedCache _cache;
        private readonly IStringLocalizer<TotpService> _localizer;
        private readonly ILogger<TotpService> _logger;
        private readonly Rfc6238AuthenticationService _rfc6238AuthenticationService;

        /// <summary>
        /// Constructs the <see cref="TotpService"/>.
        /// </summary>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="smsServiceFactory">Sms Service Factory</param>
        /// <param name="pushNotificationService">Push Notification service</param>
        /// <param name="distributedCache">Represents a distributed cache of serialized values.</param>
        /// <param name="localizer">Represents a service that provides localized strings.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        /// <param name="rfc6238AuthenticationService">Time-Based One-Time Password Algorithm service.</param>
        public TotpService(
            UserManager<User> userManager,
            ISmsServiceFactory smsServiceFactory,
            IPushNotificationService pushNotificationService,
            IDistributedCache distributedCache,
            IStringLocalizer<TotpService> localizer,
            ILogger<TotpService> logger,
            Rfc6238AuthenticationService rfc6238AuthenticationService
        ) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _smsServiceFactory = smsServiceFactory ?? throw new ArgumentNullException(nameof(smsServiceFactory));
            _pushNotificationService = pushNotificationService;
            _cache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _rfc6238AuthenticationService = rfc6238AuthenticationService ?? throw new ArgumentNullException(nameof(rfc6238AuthenticationService));
        }

        /// <inheritdoc />
        public async Task<TotpResult> Send(ClaimsPrincipal principal, string message, TotpDeliveryChannel channel = TotpDeliveryChannel.Sms, string purpose = null, string securityToken = null, string phoneNumberOrEmail = null, string data = null, string classification = null, string subject = null) {
            var totpResult = ValidateParameters(principal, securityToken, phoneNumberOrEmail);
            if (!totpResult.Success) {
                return totpResult;
            }
            User user = null;
            var hasPrincipal = principal != null;
            if (hasPrincipal) {
                user = await _userManager.GetUserAsync(principal);
                if (user?.PhoneNumberConfirmed == false || string.IsNullOrEmpty(user?.PhoneNumber)) {
                    return TotpResult.ErrorResult(_localizer["Cannot send SMS. User's phone number is not verified."]);
                }
            }
            purpose ??= TotpConstants.TokenGenerationPurpose.StrongCustomerAuthentication;
            var token = string.Empty;
            var hasSecurityToken = !string.IsNullOrEmpty(securityToken);
            if (hasSecurityToken) {
                var modifier = GetModifier(purpose, phoneNumberOrEmail);
                var encodedToken = Encoding.Unicode.GetBytes(securityToken);
                token = _rfc6238AuthenticationService.GenerateCode(encodedToken, modifier).ToString("D6", CultureInfo.InvariantCulture);
            }
            if (hasPrincipal) {
                token = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, purpose);
            }
            var userName = user?.UserName ?? "Anonymous";
            var cacheKey = $"totp{(hasPrincipal ? $":{user.Id}" : string.Empty)}:{channel}:{token}:{purpose}";
            if (await CacheKeyExists(cacheKey)) {
                _logger.LogInformation("User: '{UserName}' - Last token has not expired yet. Throttling", userName);
                return TotpResult.ErrorResult(_localizer["Last token has not expired yet. Please wait a few seconds and try again."]);
            }
            _logger.LogInformation("User: '{UserName}' - Token generated successfully", userName);
            switch (channel) {
                case TotpDeliveryChannel.Sms:
                case TotpDeliveryChannel.Viber:
                    var smsService = _smsServiceFactory.Create(channel.ToString());
                    await smsService.SendAsync(user?.PhoneNumber ?? phoneNumberOrEmail, _localizer[subject ?? "OTP"], _localizer[message, token]);
                    break;
                case TotpDeliveryChannel.Email:
                case TotpDeliveryChannel.Telephone:
                case TotpDeliveryChannel.EToken:
                    throw new NotSupportedException($"Delivery channel '{channel}' is not supported.");
                case TotpDeliveryChannel.PushNotification:
                    if (_pushNotificationService == null) {
                        throw new ArgumentNullException(nameof(_pushNotificationService), $"Cannot send push notification since there is no implementation of {nameof(IPushNotificationService)}.");
                    }
                    await _pushNotificationService.SendAsync(builder =>
                        builder.To(user?.Id)
                               .WithToken(token)
                               .WithTitle(string.Format(message, token))
                               .WithData(data)
                               .WithClassification(classification));
                    break;
                default:
                    break;
            }
            await AddCacheKey(cacheKey);
            return TotpResult.SuccessResult;
        }

        /// <inheritdoc />
        public async Task<TotpResult> Verify(ClaimsPrincipal principal, string code, TotpProviderType? provider = null, string purpose = null, string securityToken = null, string phoneNumberOrEmail = null) {
            var totpResult = ValidateParameters(principal, securityToken, phoneNumberOrEmail);
            if (!totpResult.Success) {
                return totpResult;
            }
            purpose ??= TotpConstants.TokenGenerationPurpose.StrongCustomerAuthentication;
            if (principal != null) {
                var user = await _userManager.GetUserAsync(principal);
                var providerName = provider.HasValue ? $"{provider}" : TokenOptions.DefaultPhoneProvider;
                var verified = await _userManager.VerifyUserTokenAsync(user, providerName, purpose, code);
                if (verified) {
                    await _userManager.UpdateSecurityStampAsync(user);
                    return TotpResult.SuccessResult;
                } else {
                    return TotpResult.ErrorResult(_localizer["The verification code is invalid."]);
                }
            }
            if (!string.IsNullOrEmpty(securityToken)) {
                if (!int.TryParse(code, out var codeInt)) {
                    return TotpResult.ErrorResult(_localizer["Totp must be an integer value."]);
                }
                var modifier = GetModifier(purpose, phoneNumberOrEmail);
                var encodedToken = Encoding.Unicode.GetBytes(securityToken);
                var isValidTotp = _rfc6238AuthenticationService.ValidateCode(encodedToken, codeInt, modifier);
                totpResult.Success = isValidTotp;
            }
            return totpResult;
        }

        /// <summary>
        /// Gets list of available providers for the given claims principal.
        /// </summary>
        /// <param name="principal">The user.</param>
        /// <exception cref="TotpServiceException">used to pass errors between service and the caller</exception>
        public async Task<Dictionary<string, TotpProviderMetadata>> GetProviders(ClaimsPrincipal principal) {
            var user = await _userManager.GetUserAsync(principal);
            var validProviders = await _userManager.GetValidTwoFactorProvidersAsync(user);
            var providers = new[] {
                new TotpProviderMetadata {
                    Type = TotpProviderType.Phone,
                    Channel = TotpDeliveryChannel.Sms,
                    DisplayName = "SMS",
                    CanGenerate = true
                },
                new TotpProviderMetadata {
                    Type = TotpProviderType.Phone,
                    Channel = TotpDeliveryChannel.Viber,
                    DisplayName = "Viber",
                    CanGenerate = true
                },
                new TotpProviderMetadata {
                    Type = TotpProviderType.Phone,
                    Channel = TotpDeliveryChannel.PushNotification,
                    DisplayName = "PushNotification",
                    CanGenerate = true
                },
                new TotpProviderMetadata {
                    Type = TotpProviderType.EToken,
                    Channel = TotpDeliveryChannel.EToken,
                    DisplayName = "e-Token",
                    CanGenerate = false
                }
            };
            return providers.Where(x => validProviders.Contains($"{x.Type}")).ToDictionary(x => x.Name);
        }

        private async Task AddCacheKey(string cacheKey) {
            var unixTime = DateTimeOffset.UtcNow.AddSeconds(CacheExpirationInSeconds).ToUnixTimeSeconds();
            await _cache.SetStringAsync(cacheKey, $"{unixTime}", new DistributedCacheEntryOptions {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(120)
            });
        }

        private async Task<bool> CacheKeyExists(string cacheKey) {
            var timeText = await _cache.GetStringAsync(cacheKey);
            var exists = timeText != null;
            if (exists && long.TryParse(timeText, out var unixTime)) {
                var time = DateTimeOffset.FromUnixTimeSeconds(unixTime);
                return time >= DateTimeOffset.UtcNow;
            }
            return exists;
        }

        private static string GetModifier(string purpose, string phoneNumberOrEmail) => $"{purpose}:{phoneNumberOrEmail}";

        private TotpResult ValidateParameters(ClaimsPrincipal principal, string securityToken, string phoneNumberOrEmail) {
            var hasSecurityToken = !string.IsNullOrEmpty(securityToken);
            var hasPrincipal = principal != null;
            if (hasSecurityToken && hasPrincipal) {
                return TotpResult.ErrorResult(_localizer["You can either provide a principal or your own security token."]);
            }
            var hasPhoneNumberOrEmail = !string.IsNullOrEmpty(phoneNumberOrEmail);
            if (hasSecurityToken && !hasPhoneNumberOrEmail) {
                return TotpResult.ErrorResult(_localizer["If you provide your own security token, please make sure you also provide a phone number or email."]);
            }
            return TotpResult.SuccessResult;
        }
    }
}
