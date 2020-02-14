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

namespace Indice.AspNetCore.Identity.Services
{
    /// <summary>
    /// Used to generate, send & verify time based one time passwords.
    /// </summary>
    public class TotpService : ITotpService
    {
        private const int CacheExpirationInSeconds = 30;

        /// <summary>
        /// Constructs the <see cref="TotpService"/>.
        /// </summary>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="smsService">Sms service.</param>
        /// <param name="distributedCache">Represents a distributed cache of serialized values.</param>
        /// <param name="localizer">Represents a service that provides localized strings.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public TotpService(UserManager<User> userManager, ISmsService smsService, IDistributedCache distributedCache, IStringLocalizer<TotpService> localizer, ILogger<TotpService> logger) {
            UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            SmsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
            Cache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            Localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Provides the APIs for managing user in a persistence store.
        /// </summary>
        public UserManager<User> UserManager { get; }
        /// <summary>
        /// Sms service.
        /// </summary>
        public ISmsService SmsService { get; }
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

        /// <inheritdoc />
        public async Task<TotpResult> Send(ClaimsPrincipal principal, TotpDeliveryChannel channel = TotpDeliveryChannel.Sms, string purpose = null, string securityToken = null, string phoneNumber = null, string email = null) {
            var totpResult = ValidateParameters(principal, securityToken, phoneNumber, email);
            if (!totpResult.Success) {
                return totpResult;
            }
            User user = null;
            var providedPrincipal = principal != null;
            if (providedPrincipal) {
                user = await UserManager.GetUserAsync(principal);
                if (user?.PhoneNumberConfirmed == false || string.IsNullOrEmpty(user?.PhoneNumber)) {
                    return TotpResult.ErrorResult(Localizer["Cannot send SMS. User's phone number is not verified."]);
                }
            }
            purpose ??= TotpConstants.TokenGenerationPurpose.StrongCustomerAuthentication;
            var token = string.Empty;
            var providedSecurityToken = !string.IsNullOrEmpty(securityToken);
            if (providedSecurityToken) {
                var modifier = GetModifier(purpose, phoneNumber, email);
                var encodedToken = Encoding.Unicode.GetBytes(securityToken);
                token = Rfc6238AuthenticationService.GenerateCode(encodedToken, modifier).ToString("D6", CultureInfo.InvariantCulture);
            }
            if (providedPrincipal) {
                token = await UserManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, purpose);
            }
            var userName = user?.UserName ?? "Anonymous";
            Logger.LogDebug($"User: '{userName}' - Token: '{token}'");
            var cacheKey = $"totp{(providedPrincipal ? $":{user.Id}" : string.Empty)}:{channel}:{token}:{purpose}";
            if (await CacheKeyExists(cacheKey)) {
                Logger.LogInformation($"User: '{userName}' - Last token has not expired yet. Throttling.");
                return TotpResult.ErrorResult(Localizer["Last token has not expired yet. Please wait a few seconds and try again."]);
            }
            Logger.LogInformation($"User: '{userName}' - Token generated successfully.");
            switch (channel) {
                case TotpDeliveryChannel.Sms:
                    await SmsService.SendAsync(user?.PhoneNumber ?? phoneNumber, Localizer["Chaniabank OTP"], Localizer["Chaniabank OTP Code {0}. Please fill in the one-time password to proceed.", token]);
                    break;
                case TotpDeliveryChannel.Viber:
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
        public async Task<TotpResult> Verify(ClaimsPrincipal principal, string code, TotpProviderType? provider = null, string purpose = null, string securityToken = null, string phoneNumber = null, string email = null) {
            var totpResult = ValidateParameters(principal, securityToken, phoneNumber, email);
            if (!totpResult.Success) {
                return totpResult;
            }
            var providedPrincipal = principal != null;
            purpose ??= TotpConstants.TokenGenerationPurpose.StrongCustomerAuthentication;
            if (providedPrincipal) {
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
            var providedSecurityToken = !string.IsNullOrEmpty(securityToken);
            if (providedSecurityToken) {
                if (!int.TryParse(code, out var codeInt)) {
                    return TotpResult.ErrorResult(Localizer["Totp must be an integer value."]);
                }
                var modifier = GetModifier(purpose, phoneNumber, email);
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

        private string GetModifier(string purpose, string phoneNumber, string email) {
            return $"Totp:{purpose}:{(!string.IsNullOrEmpty(phoneNumber) ? phoneNumber : email)}";
        }

        private TotpResult ValidateParameters(ClaimsPrincipal principal, string securityToken, string phoneNumber, string email) {
            var providedSecurityToken = !string.IsNullOrEmpty(securityToken);
            var providedPrincipal = principal != null;
            if (providedSecurityToken && providedPrincipal) {
                return TotpResult.ErrorResult(Localizer["You can either provide a principal or your own security token."]);
            }
            var providedPhoneNumber = !string.IsNullOrEmpty(phoneNumber);
            var providedEmail = !string.IsNullOrEmpty(email);
            if (providedSecurityToken && !providedPhoneNumber && !providedEmail) {
                return TotpResult.ErrorResult(Localizer["If you provide your own security token, please make sure you also provide a phone number or email."]);
            }
            return TotpResult.SuccessResult;
        }
    }
}
