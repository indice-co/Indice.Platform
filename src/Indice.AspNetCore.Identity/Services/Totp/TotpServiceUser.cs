using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Serialization;
using Indice.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Indice.AspNetCore.Identity
{
    /// <summary></summary>
    /// <typeparam name="TUser">The type of user entity.</typeparam>
    public class TotpServiceUser<TUser> : TotpServiceBase where TUser : User
    {
        private readonly IStringLocalizer<TotpServiceUser<TUser>> _localizer;

        /// <summary>Creates a new instance of <see cref="TotpServiceUser{TUser}"/>.</summary>
        /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
        /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="TotpServiceUser{TUser}"/>.</param>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public TotpServiceUser(
            ExtendedUserManager<TUser> userManager,
            IStringLocalizer<TotpServiceUser<TUser>> localizer,
            IServiceProvider serviceProvider
        ) : base(serviceProvider) {
            UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
        protected ExtendedUserManager<TUser> UserManager { get; }

        /// <summary>Creates a TOTP and sends it as an SMS message.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="message">The message to be sent in the SMS. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        public Task<TotpResult> SendToSmsAsync(TUser user, string message, string subject, string purpose = null)
            => SendAsync(user, message, subject, TotpDeliveryChannel.Sms, purpose: purpose);

        /// <summary>Creates a TOTP and sends it as a Viber message.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="message">The message to be sent in Viber. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        public Task<TotpResult> SendToViberAsync(TUser user, string message, string subject, string purpose = null)
            => SendAsync(user, message, subject, TotpDeliveryChannel.Viber, purpose: purpose);

        /// <summary>Creates a TOTP and sends it as a push notification.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="message">The message to be sent in push notification. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        /// <param name="classification">The notification's type.</param>
        /// <param name="data">The push notification data (preferably as a JSON string).</param>
        public Task<TotpResult> SendToPushNotificationAsync(TUser user, string message, string subject, string purpose = null, string classification = null, string data = null)
            => SendAsync(user, message, subject, TotpDeliveryChannel.PushNotification, purpose, classification, data);

        /// <summary>Creates a TOTP and sends it as a push notification.</summary>
        /// <typeparam name="TData">The type of data to send.</typeparam>
        /// <param name="user">The user instance.</param>
        /// <param name="message">The message to be sent in push notification. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="data">The data to send.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        /// <param name="classification">The notification's type.</param>
        public Task<TotpResult> SendToPushNotificationAsync<TData>(TUser user, string message, string subject, TData data, string purpose = null, string classification = null) where TData : class {
            var dataJson = JsonSerializer.Serialize(data, JsonSerializerOptionDefaults.GetDefaultSettings());
            return SendToPushNotificationAsync(user, message, subject, purpose, classification, dataJson);
        }

        /// <summary>Creates a TOTP and sends it in the selected <see cref="TotpDeliveryChannel"/>.</summary>
        /// <param name="principal">The current user principal.</param>
        /// <param name="message">The message to be sent in the selected channel. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="channel">The delivery channel.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        /// <param name="classification">The notification's type.</param>
        /// <param name="data">The push notification data (preferably as a JSON string).</param>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<TotpResult> SendAsync(
            ClaimsPrincipal principal,
            string message,
            string subject,
            TotpDeliveryChannel channel = TotpDeliveryChannel.Sms,
            string purpose = null,
            string classification = null,
            string data = null
        ) {
            var user = await UserManager.GetUserAsync(principal);
            return await SendAsync(user, message, subject, channel, purpose, classification, data);
        }

        /// <summary>Creates a TOTP and sends it in the selected <see cref="TotpDeliveryChannel"/>.</summary>
        /// <param name="configureAction"></param>
        public Task<TotpResult> SendAsync(Action<TotpServiceUserParametersBuilder<TUser>> configureAction) {
            var builder = new TotpServiceUserParametersBuilder<TUser>();
            configureAction(builder);
            var @params = builder.Build();
            if (@params.ClaimsPrincipal is not null) {
                return SendAsync(@params.ClaimsPrincipal, @params.Message, @params.Subject, @params.DeliveryChannel, @params.Purpose, @params.Classification, @params.Data);
            }
            return SendAsync(@params.User, @params.Message, @params.Subject, @params.DeliveryChannel, @params.Purpose, @params.Classification, @params.Data);
        }

        /// <summary>Creates a TOTP and sends it in the selected <see cref="TotpDeliveryChannel"/>.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="message">The message to be sent in the selected channel. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="channel">The delivery channel.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        /// <param name="classification">The notification's type.</param>
        /// <param name="data">The push notification data (preferably as a JSON string).</param>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual async Task<TotpResult> SendAsync(
            TUser user,
            string message,
            string subject,
            TotpDeliveryChannel channel = TotpDeliveryChannel.Sms,
            string purpose = null,
            string classification = null,
            string data = null
        ) {
            if (user is null) {
                throw new ArgumentNullException(nameof(user), "User is null.");
            }
            if (string.IsNullOrEmpty(user.PhoneNumber) || !user.PhoneNumberConfirmed) {
                return TotpResult.ErrorResult(_localizer["User's phone number does not exist or is not verified."]);
            }
            purpose ??= TotpConstants.TokenGenerationPurpose.StrongCustomerAuthentication;
            var token = await UserManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, purpose);
            message = _localizer[message, token];
            var cacheKey = $"{nameof(TotpServiceUser<TUser>)}:{user.Id}:{channel}:{token}:{purpose}";
            if (await CacheKeyExistsAsync(cacheKey)) {
                return TotpResult.ErrorResult(_localizer["Last token has not expired yet. Please wait a few seconds and try again."]);
            }
            if (channel == TotpDeliveryChannel.PushNotification) {
                // TODO: Find device id.
            }
            await SendToChannelAsync(channel, message, subject, user.PhoneNumber, deviceId: null, user.Id, classification, data);
            await AddCacheKeyAsync(cacheKey);
            return TotpResult.SuccessResult;
        }

        /// <summary>Verifies the TOTP received for the given claims principal.</summary>
        /// <param name="principal">The current user principal.</param>
        /// <param name="code">The TOTP code to verify.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        public async Task<TotpResult> VerifyAsync(
            ClaimsPrincipal principal,
            string code,
            string purpose = null
        ) {
            var user = await UserManager.GetUserAsync(principal);
            return await VerifyAsync(user, code, purpose);
        }

        /// <summary>Verifies the TOTP received for the given user.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="code">The TOTP code to verify.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual async Task<TotpResult> VerifyAsync(
            TUser user,
            string code,
            string purpose = null
        ) {
            if (user is null) {
                throw new ArgumentNullException(nameof(user), "User is null.");
            }
            purpose ??= TotpConstants.TokenGenerationPurpose.StrongCustomerAuthentication;
            var verified = await UserManager.VerifyUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, purpose, code);
            if (verified) {
                await UserManager.UpdateSecurityStampAsync(user);
                return TotpResult.SuccessResult;
            } else {
                return TotpResult.ErrorResult(_localizer["The verification code is invalid."]);
            }
        }

        /// <summary>Gets list of available providers for the given user.</summary>
        /// <param name="user">The user entity type.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual async Task<Dictionary<string, TotpProviderMetadata>> GetProvidersAsync(TUser user) {
            if (user is null) {
                throw new ArgumentNullException(nameof(user), "User is null.");
            }
            var validProviders = await UserManager.GetValidTwoFactorProvidersAsync(user);
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
            return providers.Where(x => validProviders.Contains(x.Type.ToString())).ToDictionary(x => x.Name);
        }

        /// <summary>Gets list of available providers for the given claims principal.</summary>
        /// <param name="principal">The user principal.</param>
        /// <exception cref="TotpServiceException">Used to pass errors between service and the caller.</exception>
        public async Task<Dictionary<string, TotpProviderMetadata>> GetProvidersAsync(ClaimsPrincipal principal) {
            var user = await UserManager.GetUserAsync(principal);
            return await GetProvidersAsync(user);
        }
    }
}
