using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Configuration;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity
{
    /// <summary>
    /// 
    /// </summary>
    public class OtpSecuredExtensionGrantValidator : IExtensionGrantValidator
    {
        private readonly ITokenValidator _tokenValidator;
        private readonly UserManager<User> _userManager;
        private readonly ISmsServiceFactory _smsServiceFactory;
        private readonly Rfc6238AuthenticationService _rfc6238AuthenticationService;
        private readonly IdentityMessageDescriber _identityMessageDescriber;

        /// <summary>
        /// Creates a new instance of <see cref="OtpSecuredExtensionGrantValidator"/>.
        /// </summary>
        /// <param name="validator">Validates an access token.</param>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="smsServiceFactory"></param>
        /// <param name="totpOptions">Configuration used in <see cref="System.Security.Rfc6238AuthenticationService"/> service.</param>
        /// <param name="identityMessageDescriber">Provides an extensibility point for altering localizing used inside the package.</param>
        public OtpSecuredExtensionGrantValidator(
            ITokenValidator validator,
            UserManager<User> userManager,
            ISmsServiceFactory smsServiceFactory,
            TotpOptions totpOptions,
            IdentityMessageDescriber identityMessageDescriber
        ) {
            _tokenValidator = validator ?? throw new ArgumentNullException(nameof(validator));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _smsServiceFactory = smsServiceFactory ?? throw new ArgumentNullException(nameof(smsServiceFactory));
            _identityMessageDescriber = identityMessageDescriber ?? throw new ArgumentNullException(nameof(identityMessageDescriber));
            _rfc6238AuthenticationService = new Rfc6238AuthenticationService(totpOptions.Timestep, totpOptions.CodeLength);
        }

        /// <inheritdoc />
        public string GrantType => CustomGrantTypes.OtpSecured;

        /// <inheritdoc />
        public async Task ValidateAsync(ExtensionGrantValidationContext context) {
            var rawRequest = context.Request.Raw;
            var accessToken = rawRequest.Get("token");
            /* 1. Check if an access token exists in the request. */
            if (string.IsNullOrWhiteSpace(accessToken)) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Access token not present in the request.");
                return;
            }
            /* 2. Validate given access token. */
            var tokenValidationResult = await _tokenValidator.ValidateAccessTokenAsync(accessToken);
            if (tokenValidationResult.IsError) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Access token is not valid.");
                return;
            }
            /* 3. Check if given access token contains a subject. */
            var subject = tokenValidationResult.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject).Value;
            if (string.IsNullOrWhiteSpace(subject)) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Claim 'sub' was not found.");
                return;
            }
            /* 4. Query the store for a user with an id equal to the one found in the token. */
            var user = await _userManager.FindByIdAsync(subject);
            if (user == null || !user.PhoneNumberConfirmed || user.Blocked) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant);
                return;
            }
            /* 5. Check if an OTP is provided in the request. */
            var modifier = $"{TotpConstants.TokenGenerationPurpose.SessionOtp}:{user.Id}:{user.PhoneNumber}";
            var securityStamp = user.SecurityStamp;
            var securityToken = Encoding.Unicode.GetBytes(securityStamp);
            var otp = rawRequest.Get("otp");
            /* 5.1 If an OTP is not provided, then we must send one to the user's confirmed phone number. */
            if (string.IsNullOrWhiteSpace(otp)) {
                /* 5.1.1 In order to send the OTP we have to decide the delivery channel. Delivery channel can optionally be sent in the request. */
                var providedChannel = rawRequest.Get("channel");
                var channel = TotpDeliveryChannel.Sms;
                if (!string.IsNullOrWhiteSpace(providedChannel)) {
                    if (Enum.TryParse<TotpDeliveryChannel>(providedChannel, out var parsedChannel)) {
                        channel = parsedChannel;
                    } else {
                        context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Invalid delivery channel.");
                        return;
                    }
                }
                var token = _rfc6238AuthenticationService.GenerateCode(securityToken, modifier).ToString("D6", CultureInfo.InvariantCulture);
                switch (channel) {
                    case TotpDeliveryChannel.Sms:
                        await _smsServiceFactory.Create(nameof(TotpDeliveryChannel.Sms)).SendAsync(user.PhoneNumber, _identityMessageDescriber.OtpSecuredValidatorOtpSubject, _identityMessageDescriber.OtpSecuredValidatorOtpBody(token));
                        break;
                    case TotpDeliveryChannel.Viber:
                        await _smsServiceFactory.Create(nameof(TotpDeliveryChannel.Viber)).SendAsync(user.PhoneNumber, _identityMessageDescriber.OtpSecuredValidatorOtpSubject, _identityMessageDescriber.OtpSecuredValidatorOtpBody(token));
                        break;
                    case TotpDeliveryChannel.Email:
                    case TotpDeliveryChannel.Telephone:
                    case TotpDeliveryChannel.EToken:
                    case TotpDeliveryChannel.PushNotification:
                    default:
                        context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Selected delivery channel is not supported.");
                        return;
                }
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "An OTP code was sent to the user. Please replay the request and include the verification code.", new Dictionary<string, object> {
                    { "otp_sent", true }
                });
                return;
            }
            /* 5.2 If an OTP is provided, then we must verify it at first. */
            var isInteger = int.TryParse(otp, out var otpInt);
            if (!isInteger) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "OTP verification code must be an integer value.");
                return;
            }
            var isValid = _rfc6238AuthenticationService.ValidateCode(securityToken, otpInt, modifier);
            /* If OTP verification code is not valid respond accordingly. */
            if (!isValid) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "OTP verification code could not be validated.");
                return;
            }
            /* If OTP verification code is valid add the same claims that were present in the token and a new one to mark that OTP verification has been successfully completed. */
            var claims = tokenValidationResult.Claims.ToList();
            claims.Add(new Claim(BasicClaimTypes.OtpVerified, "true"));
            context.Result = new GrantValidationResult(subject, GrantType, claims);
        }
    }
}
