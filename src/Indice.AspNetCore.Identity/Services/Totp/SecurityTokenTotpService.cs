using System;
using System.Globalization;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Indice.Services;
using Microsoft.Extensions.Localization;

namespace Indice.AspNetCore.Identity
{
    /// <summary></summary>
    public sealed class SecurityTokenTotpService : TotpServiceBase
    {
        private readonly Rfc6238AuthenticationService _rfc6238AuthenticationService;
        private readonly IStringLocalizer<SecurityTokenTotpService> _localizer;

        /// <summary>Creates a new instance of <see cref="SecurityTokenTotpService"/>.</summary>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
        /// <param name="rfc6238AuthenticationService">Time-Based One-Time Password Algorithm service.</param>
        /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="SecurityTokenTotpService"/>.</param>
        public SecurityTokenTotpService(
            IServiceProvider serviceProvider,
            Rfc6238AuthenticationService rfc6238AuthenticationService,
            IStringLocalizer<SecurityTokenTotpService> localizer
        ) : base(serviceProvider) {
            _rfc6238AuthenticationService = rfc6238AuthenticationService ?? throw new ArgumentNullException(nameof(rfc6238AuthenticationService));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>Creates a TOTP and sends it as an SMS message.</summary>
        /// <param name="securityToken">A security code. This should be a secret.</param>
        /// <param name="message">The message to be sent in the SMS. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="phoneNumber">The receiver's phone number.</param>
        public Task<TotpResult> SendToSmsAsync(string securityToken, string message, string subject, string phoneNumber, string purpose = null)
            => SendAsync(securityToken, message, subject, phoneNumber, TotpDeliveryChannel.Sms, purpose: purpose);

        /// <summary>Creates a TOTP and sends it as a Viber message.</summary>
        /// <param name="securityToken">A security code. This should be a secret.</param>
        /// <param name="message">The message to be sent in the SMS. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="phoneNumber">The receiver's phone number.</param>
        public Task<TotpResult> SendToViberAsync(string securityToken, string message, string subject, string phoneNumber, string purpose = null)
            => SendAsync(securityToken, message, subject, phoneNumber, TotpDeliveryChannel.Viber);

        /// <summary>Creates a TOTP and sends it as a push notification.</summary>
        /// <param name="securityToken">A security code. This should be a secret.</param>
        /// <param name="message">The message to be sent in the selected channel. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="phoneNumber">The receiver's phone number.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        /// <param name="pushNotificationClassification">The notification's type.</param>
        /// <param name="pushNotificationData">The push notification data (preferably as a JSON string).</param>
        public Task<TotpResult> SendToPushNotificationAsync(string securityToken, string message, string subject, string phoneNumber, string purpose = null, string pushNotificationClassification = null, string pushNotificationData = null)
            => SendAsync(securityToken, message, subject, phoneNumber, TotpDeliveryChannel.PushNotification, purpose, pushNotificationClassification, pushNotificationData);

        /// <summary>Creates a TOTP and sends it in the selected <see cref="TotpDeliveryChannel"/>.</summary>
        /// <param name="securityToken">A security code. This should be a secret.</param>
        /// <param name="message">The message to be sent in the selected channel. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="phoneNumber">The receiver's phone number.</param>
        /// <param name="channel">The delivery channel.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        /// <param name="pushNotificationClassification">The notification's type.</param>
        /// <param name="pushNotificationData">The push notification data (preferably as a JSON string).</param>
        public async Task<TotpResult> SendAsync(
            string securityToken,
            string message,
            string subject,
            string phoneNumber,
            TotpDeliveryChannel channel = TotpDeliveryChannel.Sms,
            string purpose = null,
            string pushNotificationClassification = null,
            string pushNotificationData = null
        ) {
            purpose ??= TotpConstants.TokenGenerationPurpose.StrongCustomerAuthentication;
            var modifier = GetModifier(purpose, phoneNumber);
            var encodedToken = Encoding.Unicode.GetBytes(securityToken);
            var token = _rfc6238AuthenticationService.GenerateCode(encodedToken, modifier).ToString("D6", CultureInfo.InvariantCulture);
            message = _localizer[message, token];
            var cacheKey = $"{nameof(SecurityTokenTotpService)}:{phoneNumber}:{channel}:{token}:{purpose}";
            if (await CacheKeyExistsAsync(cacheKey)) {
                return TotpResult.ErrorResult(_localizer["Last token has not expired yet. Please wait a few seconds and try again."]);
            }
            await SendToChannelAsync(channel, message, subject, phoneNumber, deviceId: null, userId: null, pushNotificationClassification, pushNotificationData);
            await AddCacheKeyAsync(cacheKey);
            return TotpResult.SuccessResult;
        }

        /// <summary>Verifies the TOTP received for the given user.</summary>
        /// <param name="phoneNumberOrEmail">The user instance.</param>
        /// <param name="securityToken"></param>
        /// <param name="code">The TOTP code to verify.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        public Task<TotpResult> VerifyAsync(
            string phoneNumberOrEmail,
            string securityToken,
            string code,
            string purpose = null
        ) {
            purpose ??= TotpConstants.TokenGenerationPurpose.StrongCustomerAuthentication;
            if (!int.TryParse(code, out var codeInt)) {
                return Task.FromResult(TotpResult.ErrorResult(_localizer["Totp must be an integer value."]));
            }
            var modifier = GetModifier(purpose, phoneNumberOrEmail);
            var encodedToken = Encoding.Unicode.GetBytes(securityToken);
            var isValidTotp = _rfc6238AuthenticationService.ValidateCode(encodedToken, codeInt, modifier);
            return Task.FromResult(isValidTotp ? TotpResult.SuccessResult : TotpResult.ErrorResult(_localizer["The verification code is invalid."]));
        }
    }
}
