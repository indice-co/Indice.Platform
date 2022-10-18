using System;
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
    public sealed class UserBasedTotpService<TUser> : TotpServiceBase where TUser : User
    {
        private readonly ExtendedUserManager<TUser> _userManager;
        private readonly IStringLocalizer<UserBasedTotpService<TUser>> _localizer;

        /// <summary>Creates a new instance of <see cref="UserBasedTotpService{TUser}"/>.</summary>
        /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
        /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="UserBasedTotpService{TUser}"/>.</param>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public UserBasedTotpService(
            ExtendedUserManager<TUser> userManager,
            IStringLocalizer<UserBasedTotpService<TUser>> localizer,
            IServiceProvider serviceProvider
        ) : base(serviceProvider) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>Creates a TOTP and sends it as an SMS message.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="message">The message to be sent in the SMS. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        public Task<TotpResult> SendToSmsAsync(TUser user, string message, string subject, string purpose = null) => SendAsync(user, message, subject, TotpDeliveryChannel.Sms, purpose: purpose);

        /// <summary>Creates a TOTP and sends it as a Viber message.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="message">The message to be sent in Viber. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        public Task<TotpResult> SendToViberAsync(TUser user, string message, string subject, string purpose = null) => SendAsync(user, message, subject, TotpDeliveryChannel.Viber, purpose: purpose);

        /// <summary>Creates a TOTP and sends it as a push notification.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="message">The message to be sent in push notification. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        /// <param name="pushNotificationClassification">The notification's type.</param>
        /// <param name="pushNotificationData">The push notification data (preferably as a JSON string).</param>
        public Task<TotpResult> SendToPushNotificationAsync(TUser user, string message, string subject, string purpose = null, string pushNotificationClassification = null, string pushNotificationData = null)
            => SendAsync(user, message, subject, TotpDeliveryChannel.PushNotification, purpose, pushNotificationClassification, pushNotificationData);

        /// <summary>Creates a TOTP and sends it as a push notification.</summary>
        /// <typeparam name="TData">The type of data to send.</typeparam>
        /// <param name="user">The user instance.</param>
        /// <param name="message">The message to be sent in push notification. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="data">The data to send.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        /// <param name="pushNotificationClassification">The notification's type.</param>
        /// <returns></returns>
        public Task<TotpResult> SendToPushNotificationAsync<TData>(TUser user, string message, string subject, TData data, string purpose = null, string pushNotificationClassification = null) where TData : class {
            var dataJson = JsonSerializer.Serialize(data, JsonSerializerOptionDefaults.GetDefaultSettings());
            return SendToPushNotificationAsync(user, message, subject, purpose, pushNotificationClassification, dataJson);
        }

        /// <summary>Creates a TOTP and sends it in the selected <see cref="TotpDeliveryChannel"/>.</summary>
        /// <param name="principal">The current user principal.</param>
        /// <param name="message">The message to be sent in the selected channel. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="channel">The delivery channel.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        /// <param name="pushNotificationClassification">The notification's type.</param>
        /// <param name="pushNotificationData">The push notification data (preferably as a JSON string).</param>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<TotpResult> SendAsync(
            ClaimsPrincipal principal,
            string message,
            string subject,
            TotpDeliveryChannel channel = TotpDeliveryChannel.Sms,
            string purpose = null,
            string pushNotificationClassification = null,
            string pushNotificationData = null
        ) {
            var user = await _userManager.GetUserAsync(principal);
            return await SendAsync(user, message, subject, channel, purpose, pushNotificationClassification, pushNotificationData);
        }

        /// <summary>Creates a TOTP and sends it in the selected <see cref="TotpDeliveryChannel"/>.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="message">The message to be sent in the selected channel. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="channel">The delivery channel.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        /// <param name="pushNotificationClassification">The notification's type.</param>
        /// <param name="pushNotificationData">The push notification data (preferably as a JSON string).</param>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<TotpResult> SendAsync(
            TUser user,
            string message,
            string subject,
            TotpDeliveryChannel channel = TotpDeliveryChannel.Sms,
            string purpose = null,
            string pushNotificationClassification = null,
            string pushNotificationData = null
        ) {
            if (user is null) {
                throw new ArgumentNullException(nameof(user), "User is null.");
            }
            if (string.IsNullOrEmpty(user.PhoneNumber) || !user.PhoneNumberConfirmed) {
                return TotpResult.ErrorResult(_localizer["User's phone number does not exist or is not verified."]);
            }
            purpose ??= TotpConstants.TokenGenerationPurpose.StrongCustomerAuthentication;
            var token = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, purpose);
            message = _localizer[message, token];
            var cacheKey = $"{nameof(UserBasedTotpService<TUser>)}:{user.Id}:{channel}:{token}:{purpose}";
            if (await CacheKeyExistsAsync(cacheKey)) {
                return TotpResult.ErrorResult(_localizer["Last token has not expired yet. Please wait a few seconds and try again."]);
            }
            await SendToChannelAsync(channel, message, subject, user.PhoneNumber, deviceId: null, user.Id, pushNotificationClassification, pushNotificationData);
            await AddCacheKeyAsync(cacheKey);
            return TotpResult.SuccessResult;
        }
    }
}
