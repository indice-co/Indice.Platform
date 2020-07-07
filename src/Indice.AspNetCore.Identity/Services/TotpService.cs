using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Models;
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

        /// <summary>
        /// Constructs the <see cref="TotpService"/>.
        /// </summary>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="smsServiceFactory">Sms Service Factory</param>
        /// <param name="distributedCache">Represents a distributed cache of serialized values.</param>
        /// <param name="localizer">Represents a service that provides localized strings.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        /// <param name="rfc6238AuthenticationService">Time-Based One-Time Password Algorithm service.</param>
        public TotpService(UserManager<User> userManager, ISmsServiceFactory smsServiceFactory, IDistributedCache distributedCache, IStringLocalizer<TotpService> localizer, ILogger<TotpService> logger, Rfc6238AuthenticationService rfc6238AuthenticationService) {
            UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            SmsServiceFactory = smsServiceFactory ?? throw new ArgumentNullException(nameof(smsServiceFactory));
            Cache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            Localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Rfc6238AuthenticationService = rfc6238AuthenticationService ?? throw new ArgumentNullException(nameof(rfc6238AuthenticationService));
        }

        /// <summary>
        /// Provides the APIs for managing user in a persistence store.
        /// </summary>
        public UserManager<User> UserManager { get; }

        /// <summary>
        /// Sms service factory.
        /// </summary>
        public ISmsServiceFactory SmsServiceFactory { get; }

        /// <summary>
        /// Represents a distributed cache of serialized values.
        /// </summary>
        public IDistributedCache Cache { get; }
        /// <summary>
        /// Represents a service that provides localized strings.
        /// </summary>
        public IStringLocalizer<TotpService> Localizer { get; }
        /// <summary>
        /// Represents a type used to perform logging.
        /// </summary>
        public ILogger<TotpService> Logger { get; }
        /// <summary>
        /// Time-Based One-Time Password Algorithm service.
        /// </summary>
        public Rfc6238AuthenticationService Rfc6238AuthenticationService { get; }

        /// <inheritdoc />
        public async Task<TotpResult> Send(ClaimsPrincipal principal, string message, TotpDeliveryChannel channel = TotpDeliveryChannel.Sms, string purpose = null, string securityToken = null, string phoneNumberOrEmail = null) {
            var totpResult = ValidateParameters(principal, securityToken, phoneNumberOrEmail);
            if (!totpResult.Success) {
                return totpResult;
            }
            User user = null;
            var hasPrincipal = principal != null;
            if (hasPrincipal) {
                user = await UserManager.GetUserAsync(principal);
                if (user?.PhoneNumberConfirmed == false || string.IsNullOrEmpty(user?.PhoneNumber)) {
                    return TotpResult.ErrorResult(Localizer["Cannot send SMS. User's phone number is not verified."]);
                }
            }
            purpose ??= TotpConstants.TokenGenerationPurpose.StrongCustomerAuthentication;
            var token = string.Empty;
            var hasSecurityToken = !string.IsNullOrEmpty(securityToken);
            if (hasSecurityToken) {
                var modifier = GetModifier(purpose, phoneNumberOrEmail);
                var encodedToken = Encoding.Unicode.GetBytes(securityToken);
                token = Rfc6238AuthenticationService.GenerateCode(encodedToken, modifier).ToString("D6", CultureInfo.InvariantCulture);
            }
            if (hasPrincipal) {
                token = await UserManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, purpose);
            }
            var userName = user?.UserName ?? "Anonymous";
            var cacheKey = $"totp{(hasPrincipal ? $":{user.Id}" : string.Empty)}:{channel}:{token}:{purpose}";
            if (await CacheKeyExists(cacheKey)) {
                Logger.LogInformation($"User: '{userName}' - Last token has not expired yet. Throttling.");
                return TotpResult.ErrorResult(Localizer["Last token has not expired yet. Please wait a few seconds and try again."]);
            }
            Logger.LogInformation($"User: '{userName}' - Token generated successfully.");

            switch (channel) {
                case TotpDeliveryChannel.Sms:
                case TotpDeliveryChannel.Viber:
                    var smsService = SmsServiceFactory.Create(channel.ToString());
                    await smsService.SendAsync(user?.PhoneNumber ?? phoneNumberOrEmail, Localizer["OTP"], Localizer[message, token]);
                    break;
                case TotpDeliveryChannel.Email:
                case TotpDeliveryChannel.Telephone:
                case TotpDeliveryChannel.EToken:
                    throw new NotSupportedException($"EToken delivery channel {channel} is not implemented.");
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
                var user = await UserManager.GetUserAsync(principal);
                var providerName = provider.HasValue ? $"{provider}" : TokenOptions.DefaultPhoneProvider;
                var verified = await UserManager.VerifyUserTokenAsync(user, providerName, purpose, code);
                if (verified) {
                    await UserManager.UpdateSecurityStampAsync(user);
                    return TotpResult.SuccessResult;
                } else {
                    return TotpResult.ErrorResult(Localizer["The verification code is invalid."]);
                }
            }
            if (!string.IsNullOrEmpty(securityToken)) {
                if (!int.TryParse(code, out var codeInt)) {
                    return TotpResult.ErrorResult(Localizer["Totp must be an integer value."]);
                }
                var modifier = GetModifier(purpose, phoneNumberOrEmail);
                var encodedToken = Encoding.Unicode.GetBytes(securityToken);
                var isValidTotp = Rfc6238AuthenticationService.ValidateCode(encodedToken, codeInt, modifier);
                totpResult.Success = isValidTotp;
            }
            return totpResult;
        }

        /// <summary>
        /// Gets list of available providers for the given claims principal.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <exception cref="TotpServiceException">used to pass errors between service and the caller</exception>
        public async Task<Dictionary<string, TotpProviderMetadata>> GetProviders(ClaimsPrincipal user) {
            var _user = await UserManager.GetUserAsync(user);
            var validProviders = await UserManager.GetValidTwoFactorProvidersAsync(_user);
            var providers = new[] {
                new TotpProviderMetadata {
                    Type = TotpProviderType.Phone,
                    Channel = TotpDeliveryChannel.Sms,
                    DisplayName = "SMS",
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
            await Cache.SetStringAsync(cacheKey, $"{unixTime}", new DistributedCacheEntryOptions {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(120)
            });
        }

        private async Task<bool> CacheKeyExists(string cacheKey) {
            var timeText = await Cache.GetStringAsync(cacheKey);
            var exists = timeText != null;
            if (exists && long.TryParse(timeText, out var unixTime)) {
                var time = DateTimeOffset.FromUnixTimeSeconds(unixTime);
                return time >= DateTimeOffset.UtcNow;
            }
            return exists;
        }

        private string GetModifier(string purpose, string phoneNumberOrEmail) => $"{purpose}:{phoneNumberOrEmail}";

        private TotpResult ValidateParameters(ClaimsPrincipal principal, string securityToken, string phoneNumberOrEmail) {
            var hasSecurityToken = !string.IsNullOrEmpty(securityToken);
            var hasPrincipal = principal != null;
            if (hasSecurityToken && hasPrincipal) {
                return TotpResult.ErrorResult(Localizer["You can either provide a principal or your own security token."]);
            }
            var hasPhoneNumberOrEmail = !string.IsNullOrEmpty(phoneNumberOrEmail);
            if (hasSecurityToken && !hasPhoneNumberOrEmail) {
                return TotpResult.ErrorResult(Localizer["If you provide your own security token, please make sure you also provide a phone number or email."]);
            }
            return TotpResult.SuccessResult;
        }
    }
}
