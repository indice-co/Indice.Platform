using System;
using System.Globalization;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Indice.Configuration;
using Indice.Services;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.Core.Totp
{
    /// <summary></summary>
    public sealed class TotpServiceSecurityToken : TotpServiceBase
    {
        private readonly Rfc6238AuthenticationService _rfc6238AuthenticationService;
        private readonly IStringLocalizer<TotpServiceSecurityToken> _localizer;

        /// <summary>Creates a new instance of <see cref="TotpServiceSecurityToken"/>.</summary>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
        /// <param name="rfc6238AuthenticationService">Time-Based One-Time Password Algorithm service.</param>
        /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="TotpServiceSecurityToken"/>.</param>
        public TotpServiceSecurityToken(
            IServiceProvider serviceProvider,
            Rfc6238AuthenticationService rfc6238AuthenticationService,
            IStringLocalizer<TotpServiceSecurityToken> localizer
        ) : base(serviceProvider) {
            _rfc6238AuthenticationService = rfc6238AuthenticationService ?? throw new ArgumentNullException(nameof(rfc6238AuthenticationService));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>Creates a TOTP and sends it in the selected <see cref="TotpDeliveryChannel"/>.</summary>
        /// <param name="configureAction"></param>
        public Task<TotpResult> SendAsync(Action<TotpServiceSecurityTokenParametersBuilder> configureAction) {
            var builder = new TotpServiceSecurityTokenParametersBuilder();
            configureAction(builder);
            var @params = builder.Build();
            return SendAsync(@params.SecurityToken, @params.Message, @params.Subject, @params.PhoneNumber, @params.DeliveryChannel, @params.Purpose);
        }

        /// <summary>Creates a TOTP and sends it in the selected <see cref="TotpDeliveryChannel"/>.</summary>
        /// <param name="securityToken">A security code. This should be a secret.</param>
        /// <param name="message">The message to be sent in the selected channel. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="phoneNumber">The receiver's phone number.</param>
        /// <param name="channel">The delivery channel.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        public async Task<TotpResult> SendAsync(
            string securityToken,
            string message,
            string subject,
            string phoneNumber,
            TotpDeliveryChannel channel = TotpDeliveryChannel.Sms,
            string purpose = null
        ) {
            purpose ??= TotpConstants.TokenGenerationPurpose.StrongCustomerAuthentication;
            var modifier = GetModifier(purpose, phoneNumber);
            var encodedToken = Encoding.Unicode.GetBytes(securityToken);
            var token = _rfc6238AuthenticationService.GenerateCode(encodedToken, modifier).ToString("D6", CultureInfo.InvariantCulture);
            message = _localizer[message, token];
            var cacheKey = $"{nameof(TotpServiceSecurityToken)}:{phoneNumber}:{channel}:{token}:{purpose}";
            if (await CacheKeyExistsAsync(cacheKey)) {
                return TotpResult.ErrorResult(_localizer["Last token has not expired yet. Please wait a few seconds and try again."]);
            }
            await SendToChannelAsync(
                channel,
                new TotpRecipient {
                    PhoneNumber = phoneNumber
                },
                new TotpMessage {
                    Message = message,
                    Subject = subject
                }
            );
            await AddCacheKeyAsync(cacheKey);
            return TotpResult.SuccessResult;
        }

        /// <summary>Verifies the TOTP received for the given user.</summary>
        /// <param name="securityToken">A security code. This should be a secret.</param>
        /// <param name="phoneNumber">The receiver's phone number.</param>
        /// <param name="code">The TOTP code to verify.</param>
        /// <param name="purpose">Optional reason to generate the TOTP.</param>
        public Task<TotpResult> VerifyAsync(
            string securityToken,
            string phoneNumber,
            string code,
            string purpose = null
        ) {
            purpose ??= TotpConstants.TokenGenerationPurpose.StrongCustomerAuthentication;
            if (!int.TryParse(code, out var codeInt)) {
                return Task.FromResult(TotpResult.ErrorResult(_localizer["Totp must be an integer value."]));
            }
            var modifier = GetModifier(purpose, phoneNumber);
            var encodedToken = Encoding.Unicode.GetBytes(securityToken);
            var isValidTotp = _rfc6238AuthenticationService.ValidateCode(encodedToken, codeInt, modifier);
            return Task.FromResult(isValidTotp ? TotpResult.SuccessResult : TotpResult.ErrorResult(_localizer["The verification code is invalid."]));
        }
    }
}
