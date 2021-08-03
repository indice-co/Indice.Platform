using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.Security;

namespace Indice.Services
{
    /// <summary>
    /// Used to generate, send and verify time based one time passwords.
    /// </summary>
    public interface ITotpService
    {
        /// <summary>
        /// Sends a new code via the selected channel for the given <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/>.</param>
        /// <param name="message">The message to be sent in the SMS. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="channel">Delivery channel.</param>
        /// <param name="purpose">Optionally pass the reason to generate the TOTP.</param>
        /// <param name="securityToken">The generated security token to use, if no <paramref name="principal"/> is provided.</param>
        /// <param name="phoneNumberOrEmail">The phone number to use, if no <paramref name="principal"/> is provided.</param>
        /// <param name="data">The json string that will be passed as data to the <see cref="IPushNotificationService"/>. It's important for the data to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <exception cref="TotpServiceException">Used to pass errors between service and the caller.</exception>
        Task<TotpResult> Send(ClaimsPrincipal principal, string message, TotpDeliveryChannel channel = TotpDeliveryChannel.Sms, string purpose = null, string securityToken = null, string phoneNumberOrEmail = null, string data = null);
        /// <summary>
        /// Verify the code received for the given claims principal.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/>.</param>
        /// <param name="code">The TOTP code.</param>
        /// <param name="provider">Optionally pass the provider to use to verify. Defaults to DefaultPhoneProvider.</param>
        /// <param name="purpose">Optionally pass the reason used to generate the TOTP.</param>
        /// <param name="securityToken">The generated security token to use, if no <paramref name="principal"/> is provided.</param>
        /// <param name="phoneNumberOrEmail">The phone number to use, if no <paramref name="principal"/> is provided.</param>
        /// <exception cref="TotpServiceException">Used to pass errors between service and the caller.</exception>
        Task<TotpResult> Verify(ClaimsPrincipal principal, string code, TotpProviderType? provider = null, string purpose = null, string securityToken = null, string phoneNumberOrEmail = null);
        /// <summary>
        /// Gets list of available providers for the given claims principal.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/>.</param>
        /// <exception cref="TotpServiceException">used to pass errors between service and the caller.</exception>
        Task<Dictionary<string, TotpProviderMetadata>> GetProviders(ClaimsPrincipal principal);
    }

    /// <summary>
    /// Extensions on <see cref="ITotpService"/>.
    /// </summary>
    public static class ITotpServiceExtensions
    {
        /// <summary>
        /// Sends a new code via the selected channel for the given user id.
        /// </summary>
        /// <param name="service">The service to use.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="message">The message to be sent in the SMS. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="channel">Delivery channel.</param>
        /// <param name="reason">Optionaly pass the reason to generate the TOTP.</param>
        /// <exception cref="TotpServiceException">used to pass errors between service and the caller.</exception>
        public static Task<TotpResult> Send(this ITotpService service, string userId, string message, TotpDeliveryChannel channel = TotpDeliveryChannel.Sms, string reason = null) =>
            service.Send(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(BasicClaimTypes.Subject, userId) })), message, channel, reason);

        /// <summary>
        /// Sends a new code via the selected channel.
        /// </summary>
        /// <param name="service">The service to use.</param>
        /// <param name="configureMessage">The delegate that will be used to build the message.</param>
        /// <exception cref="TotpServiceException">used to pass errors between service and the caller.</exception>
        public static Task<TotpResult> Send(this ITotpService service, Action<TotpMessageBuilder> configureMessage) {
            if (configureMessage == null) {
                throw new ArgumentNullException(nameof(configureMessage));
            }
            var messageBuilder = new TotpMessageBuilder();
            configureMessage(messageBuilder);
            var totpMessage = messageBuilder.Build();
            return service.Send(totpMessage.ClaimsPrincipal, totpMessage.Message, totpMessage.DeliveryChannel, totpMessage.Purpose, totpMessage.SecurityToken, totpMessage.PhoneNumberOrEmail, totpMessage.Data);
        }

        /// <summary>
        /// Verify the code received for the given user id.
        /// </summary>
        /// <param name="service">The service to use.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="code">The TOTP code.</param>
        /// <param name="provider">Optionally pass the provider to use to verify. Defaults to DefaultPhoneProvider</param>
        /// <param name="reason">Optionally pass the reason used to generate the TOTP.</param>
        public static Task<TotpResult> Verify(this ITotpService service, string userId, string code, TotpProviderType? provider = null, string reason = null) =>
            service.Verify(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(BasicClaimTypes.Subject, userId) })), code, provider, reason);

        /// <summary>
        /// Gets list of available providers for the given claims principal.
        /// </summary>
        /// <param name="service">The service to use.</param>
        /// <param name="userId">The user id.</param>
        public static Task<Dictionary<string, TotpProviderMetadata>> GetProviders(this ITotpService service, string userId) =>
            service.GetProviders(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(BasicClaimTypes.Subject, userId) })));
    }

    #region Builder Classes
    /// <summary>
    /// Builder for <see cref="TotpMessage"/>.
    /// </summary>
    public class TotpMessageBuilder
    {
        /// <summary>
        /// The claims principal.
        /// </summary>
        public ClaimsPrincipal ClaimsPrincipal { get; internal set; }
        /// <summary>
        /// The message to be sent in the SMS. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.
        /// If the <see cref="DeliveryChannel"/> is PushNotification, the {0} placeholder can be ignored and use a human friendly message.
        /// </summary>
        public string Message { get; internal set; }
        /// <summary>
        /// The payload data as json string to be sent in PushNotification. It's important for the data to contain the {0} placeholder in the position where the OTP should be placed.
        /// </summary>
        public string Data { get; internal set; }
        /// <summary>
        /// Security token.
        /// </summary>
        public string SecurityToken { get; internal set; }
        /// <summary>
        /// Email address or phone number.
        /// </summary>
        public string PhoneNumberOrEmail { get; internal set; }
        /// <summary>
        /// Chosen delivery channel.
        /// </summary>
        public TotpDeliveryChannel DeliveryChannel { get; internal set; } = TotpDeliveryChannel.Sms;
        /// <summary>
        /// The purpose.
        /// </summary>
        public string Purpose { get; internal set; } = TotpConstants.TokenGenerationPurpose.StrongCustomerAuthentication;

        /// <summary>
        /// Sets the <see cref="ClaimsPrincipal"/> property.
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal.</param>
        /// <returns>The <see cref="ITotpContactBuilder"/>.</returns>
        public ITotpMessageContentBuilder UsePrincipal(ClaimsPrincipal claimsPrincipal) {
            ClaimsPrincipal = claimsPrincipal ?? throw new ArgumentNullException($"Parameter {nameof(claimsPrincipal)} cannot be null.");
            var totpMessageContentBuilder = new TotpMessageContentBuilder(this);
            return totpMessageContentBuilder;
        }

        /// <summary>
        /// Sets the <see cref="SecurityToken"/> property.
        /// </summary>
        /// <param name="securityToken">Security token.</param>
        /// <returns>The <see cref="ITotpContactBuilder"/>.</returns>
        public ITotpMessageContentBuilder UseSecurityToken(string securityToken) {
            if (string.IsNullOrEmpty(securityToken)) {
                throw new ArgumentNullException(nameof(securityToken), $"Parameter {nameof(securityToken)} cannot be null or empty.");
            }
            SecurityToken = securityToken;
            var totpMessageContentBuilder = new TotpMessageContentBuilder(this);
            return totpMessageContentBuilder;
        }

        /// <summary>
        /// Builds the <see cref="TotpMessage"/>.
        /// </summary>
        public TotpMessage Build() => new() {
            ClaimsPrincipal = ClaimsPrincipal,
            Message = Message,
            Data = Data,
            SecurityToken = SecurityToken,
            PhoneNumberOrEmail = PhoneNumberOrEmail,
            DeliveryChannel = DeliveryChannel,
            Purpose = Purpose
        };
    }

    /// <summary>
    /// Builder for the <see cref="TotpDataBuilder"/>.
    /// </summary>
    public interface ITotpDataBuilder
    {
        /// <summary>
        /// Sets the <see cref="TotpMessageBuilder.Data"/> property.
        /// </summary>
        /// <param name="data">The payload data as json string to be sent in PushNotification. It's important for the data to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <returns></returns>
        void WithData(string data);
    }

    /// <inheritdoc />
    public class TotpDataBuilder : ITotpDataBuilder
    {
        private readonly TotpMessageBuilder _totpMessageBuilder;

        /// <summary>
        /// Creates a new instance of <see cref="TotpDataBuilder"/>.
        /// </summary>
        /// <param name="totpMessageBuilder">The instance of <see cref="TotpMessageBuilder"/>.</param>
        public TotpDataBuilder(TotpMessageBuilder totpMessageBuilder) {
            _totpMessageBuilder = totpMessageBuilder ?? throw new ArgumentNullException(nameof(totpMessageBuilder));
        }

        /// <inheritdoc/>
        public void WithData(string data) {
            _totpMessageBuilder.Data = data;
        }
    }

    /// <summary>
    /// Builder for <see cref="TotpMessage"/>.
    /// </summary>
    public interface ITotpMessageContentBuilder
    {
        /// <summary>
        /// Sets the <see cref="TotpMessageBuilder.Message"/> property.
        /// </summary>
        /// <param name="message">The message to be sent in the SMS. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <returns></returns>
        ITotpContactBuilder WithMessage(string message);
    }

    /// <inheritdoc/>
    public class TotpMessageContentBuilder : ITotpMessageContentBuilder
    {
        private readonly TotpMessageBuilder _totpMessageBuilder;

        /// <summary>
        /// Creates a new instance of <see cref="TotpPurposeBuilder"/>.
        /// </summary>
        /// <param name="totpMessageBuilder">The instance of <see cref="TotpMessageBuilder"/>.</param>
        public TotpMessageContentBuilder(TotpMessageBuilder totpMessageBuilder) {
            _totpMessageBuilder = totpMessageBuilder ?? throw new ArgumentNullException(nameof(totpMessageBuilder));
        }

        /// <inheritdoc/>
        public ITotpContactBuilder WithMessage(string message) {
            if (string.IsNullOrEmpty(message)) {
                throw new ArgumentNullException(nameof(message), $"Parameter {nameof(message)} cannot be null or empty.");
            }
            _totpMessageBuilder.Message = message;
            var totpContactBuilder = new TotpContactBuilder(_totpMessageBuilder);
            return totpContactBuilder;
        }
    }

    /// <summary>
    /// Builder for <see cref="TotpMessage"/>.
    /// </summary>
    public interface ITotpContactBuilder : ITotpPhoneProviderBuilder
    {
        /// <summary>
        /// Sets the <see cref="TotpMessageBuilder.PhoneNumberOrEmail"/> property.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <returns></returns>
        ITotpPurposeBuilder ToEmail(string email);
        /// <summary>
        /// Sets the <see cref="TotpMessageBuilder.PhoneNumberOrEmail"/> property.
        /// </summary>
        /// <param name="phoneNumber">Phone number.</param>
        /// <returns></returns>
        ITotpPhoneProviderBuilder ToPhoneNumber(string phoneNumber);
        /// <summary>
        /// Sets the <see cref="TotpMessageBuilder.Purpose"/> property.
        /// </summary>
        /// <param name="purpose">The purpose.</param>
        void WithPurpose(string purpose);
    }

    /// <inheritdoc cref="ITotpContactBuilder" />
    public class TotpContactBuilder : TotpPhoneProviderBuilder, ITotpContactBuilder
    {
        private readonly TotpMessageBuilder _totpMessageBuilder;

        /// <summary>
        /// Creates a new instance of <see cref="TotpPurposeBuilder"/>.
        /// </summary>
        /// <param name="totpMessageBuilder">The instance of <see cref="TotpMessageBuilder"/>.</param>
        public TotpContactBuilder(TotpMessageBuilder totpMessageBuilder) : base(totpMessageBuilder) {
            _totpMessageBuilder = totpMessageBuilder ?? throw new ArgumentNullException(nameof(totpMessageBuilder));
        }

        /// <inheritdoc/>
        public ITotpPurposeBuilder ToEmail(string email) {
            if (string.IsNullOrEmpty(email)) {
                throw new ArgumentNullException(nameof(email), $"Parameter {nameof(email)} cannot be null or empty.");
            }
            _totpMessageBuilder.PhoneNumberOrEmail = email;
            var totpPurposeBuilder = new TotpPurposeBuilder(_totpMessageBuilder);
            return totpPurposeBuilder;
        }

        /// <inheritdoc/>
        public ITotpPhoneProviderBuilder ToPhoneNumber(string phoneNumber) {
            if (string.IsNullOrEmpty(phoneNumber)) {
                throw new ArgumentNullException(nameof(phoneNumber), $"Parameter {nameof(phoneNumber)} cannot be null or empty.");
            }
            _totpMessageBuilder.PhoneNumberOrEmail = phoneNumber;
            var totpPhoneProviderBuilder = new TotpPhoneProviderBuilder(_totpMessageBuilder);
            return totpPhoneProviderBuilder;
        }

        /// <inheritdoc/>
        public void WithPurpose(string purpose) {
            if (string.IsNullOrEmpty(purpose)) {
                throw new ArgumentNullException(nameof(purpose), $"Parameter {nameof(purpose)} cannot be null or empty.");
            }
            _totpMessageBuilder.Purpose = purpose;
        }
    }

    /// <summary>
    /// Builder for <see cref="TotpMessage"/>.
    /// </summary>
    public interface ITotpPurposeBuilder
    {
        /// <summary>
        /// Sets the <see cref="TotpMessageBuilder.Purpose"/> property.
        /// </summary>
        /// <param name="purpose">The purpose.</param>
        ITotpDataBuilder WithPurpose(string purpose);
        
    }

    /// <inheritdoc/>
    public class TotpPurposeBuilder : ITotpPurposeBuilder
    {
        private readonly TotpMessageBuilder _totpMessageBuilder;

        /// <summary>
        /// Creates a new instance of <see cref="TotpPurposeBuilder"/>.
        /// </summary>
        /// <param name="totpMessageBuilder">The instance of <see cref="TotpMessageBuilder"/>.</param>
        public TotpPurposeBuilder(TotpMessageBuilder totpMessageBuilder) {
            _totpMessageBuilder = totpMessageBuilder ?? throw new ArgumentNullException(nameof(totpMessageBuilder));
        }

        /// <inheritdoc/>
        public ITotpDataBuilder WithPurpose(string purpose) {
            if (string.IsNullOrEmpty(purpose)) {
                throw new ArgumentNullException(nameof(purpose), $"Parameter {nameof(purpose)} cannot be null or empty.");
            }
            _totpMessageBuilder.Purpose = purpose;
            var dataBuilder = new TotpDataBuilder(_totpMessageBuilder);
            return dataBuilder;
        }
    }

    /// <summary>
    /// Builder for <see cref="TotpMessage"/>.
    /// </summary>
    public interface ITotpPhoneProviderBuilder
    {
        /// <summary>
        /// Sets the <see cref="TotpMessageBuilder.DeliveryChannel"/> property.
        /// </summary>
        /// <returns></returns>
        ITotpPurposeBuilder UsingSms();
        /// <summary>
        /// Sets the <see cref="TotpMessageBuilder.DeliveryChannel"/> property.
        /// </summary>
        /// <returns></returns>
        ITotpPurposeBuilder UsingViber();
        /// <summary>
        /// Sets the <see cref="TotpMessageBuilder.DeliveryChannel"/> property.
        /// </summary>
        /// <returns></returns>
        ITotpPurposeBuilder UsingTelephone();
        /// <summary>
        /// Sets the <see cref="TotpMessageBuilder.DeliveryChannel"/> property.
        /// </summary>
        /// <returns></returns>
        ITotpPurposeBuilder UsingPushNotification();
    }

    /// <inheritdoc/>
    public class TotpPhoneProviderBuilder : ITotpPhoneProviderBuilder
    {
        private readonly TotpMessageBuilder _totpMessageBuilder;

        /// <summary>
        /// Creates a new instance of <see cref="TotpPurposeBuilder"/>.
        /// </summary>
        /// <param name="totpMessageBuilder">The instance of <see cref="TotpMessageBuilder"/>.</param>
        public TotpPhoneProviderBuilder(TotpMessageBuilder totpMessageBuilder) {
            _totpMessageBuilder = totpMessageBuilder ?? throw new ArgumentNullException(nameof(totpMessageBuilder));
        }

        /// <inheritdoc/>
        public ITotpPurposeBuilder UsingSms() {
            _totpMessageBuilder.DeliveryChannel = TotpDeliveryChannel.Sms;
            var totpPurposeBuilder = new TotpPurposeBuilder(_totpMessageBuilder);
            return totpPurposeBuilder;
        }

        /// <inheritdoc/>
        public ITotpPurposeBuilder UsingTelephone() {
            _totpMessageBuilder.DeliveryChannel = TotpDeliveryChannel.Telephone;
            var totpPurposeBuilder = new TotpPurposeBuilder(_totpMessageBuilder);
            return totpPurposeBuilder;
        }

        /// <inheritdoc/>
        public ITotpPurposeBuilder UsingViber() {
            _totpMessageBuilder.DeliveryChannel = TotpDeliveryChannel.Viber;
            var totpPurposeBuilder = new TotpPurposeBuilder(_totpMessageBuilder);
            return totpPurposeBuilder;
        }

        /// <inheritdoc/>
        public ITotpPurposeBuilder UsingPushNotification() {
            _totpMessageBuilder.DeliveryChannel = TotpDeliveryChannel.PushNotification;
            var totpPurposeBuilder = new TotpPurposeBuilder(_totpMessageBuilder);
            return totpPurposeBuilder;
        }
    }
    #endregion

    #region Models Supporting ITotpService
    /// <summary>
    /// <see cref="ITotpService"/> result.
    /// </summary>
    public class TotpResult
    {
        /// <summary>
        /// Constructs an error result.
        /// </summary>
        /// <param name="error">The error.</param>
        public static TotpResult ErrorResult(string error) {
            return new TotpResult {
                Error = error
            };
        }

        /// <summary>
        /// Constructs a success result.
        /// </summary>
        public static TotpResult SuccessResult => new() { Success = true };
        /// <summary>
        /// Indicates success.
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// The error occured.
        /// </summary>
        public string Error { get; set; }
    }

    /// <summary>
    /// Specific exception used to pass errors between <see cref="ITotpService"/> and the caller.
    /// </summary>
    [Serializable]
    public class TotpServiceException : Exception
    {
        /// <summary>
        /// Constructs the <see cref="TotpServiceException"/>.
        /// </summary>
        public TotpServiceException() { }

        /// <summary>
        /// Constructs the <see cref="TotpServiceException"/>.
        /// </summary>
        public TotpServiceException(string message) : base(message) { }

        /// <summary>
        /// Constructs the <see cref="TotpServiceException"/>.
        /// </summary>
        public TotpServiceException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Constructs the <see cref="TotpServiceException"/>.
        /// </summary>
        protected TotpServiceException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// TOTP provider metadata.
    /// </summary>
    public class TotpProviderMetadata
    {
        /// <summary>
        /// The provider type.
        /// </summary>
        public TotpProviderType Type { get; set; }
        /// <summary>
        /// The provider channel.
        /// </summary>
        public TotpDeliveryChannel Channel { get; set; }
        /// <summary>
        /// The name which is used to register the provider in the list of token providers.
        /// </summary>
        public string Name => $"{Channel}";
        /// <summary>
        /// The display name.
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Can generate TOTP. If false this provider can only validate TOTPs.
        /// </summary>
        public bool CanGenerate { get; set; }
    }

    /// <summary>
    /// Supported TOTP delivery factors.
    /// </summary>
    public enum TotpDeliveryChannel
    {
        /// <summary>
        /// SMS
        /// </summary>
        Sms = 1,
        /// <summary>
        /// Email
        /// </summary>
        Email = 2,
        /// <summary>
        /// Telephone
        /// </summary>
        Telephone = 3,
        /// <summary>
        /// Viber
        /// </summary>
        Viber = 4,
        /// <summary>
        /// E-token
        /// </summary>
        EToken = 5,
        /// <summary>
        /// Push notification
        /// </summary>
        PushNotification = 6,
        /// <summary>
        /// None
        /// </summary>
        None
    }

    /// <summary>
    /// Supported TOTP providers used for MFA.
    /// </summary>
    public enum TotpProviderType
    {
        /// <summary>
        /// Phone.
        /// </summary>
        Phone = 1,
        /// <summary>
        /// E-token.
        /// </summary>
        EToken = 3,
        /// <summary>
        /// Standard OTP.
        /// </summary>
        StandardOtp = 4
    }

    /// <summary>
    /// Model for a TOTP message.
    /// </summary>
    public class TotpMessage
    {
        /// <summary>
        /// The claims principal.
        /// </summary>
        public ClaimsPrincipal ClaimsPrincipal { get; set; }
        /// <summary>
        /// The message to be sent in the SMS. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.
        /// If the <see cref="DeliveryChannel"/> is PushNotification, the {0} placeholder can be ignored and use a human friendly message.
        /// </summary>
        public string Message { get; internal set; }
        /// <summary>
        /// The payload data as json string to be sent in PushNotification. It's important for the data to contain the {0} placeholder in the position where the OTP should be placed.
        /// </summary>
        public string Data { get; internal set; }
        /// <summary>
        /// Chosen delivery channel.
        /// </summary>
        public TotpDeliveryChannel DeliveryChannel { get; set; } = TotpDeliveryChannel.Sms;
        /// <summary>
        /// The purpose.
        /// </summary>
        public string Purpose { get; set; } = TotpConstants.TokenGenerationPurpose.StrongCustomerAuthentication;
        /// <summary>
        /// Security token.
        /// </summary>
        public string SecurityToken { get; set; }
        /// <summary>
        /// Email address or phone number.
        /// </summary>
        public string PhoneNumberOrEmail { get; set; }
    }

    /// <summary>
    /// Constant values for <see cref="ITotpService"/>.
    /// </summary>
    public static class TotpConstants
    {
        /// <summary>
        /// Token generation purpose.
        /// </summary>
        public static class TokenGenerationPurpose
        {
            /// <summary>
            /// SCA
            /// </summary>
            public const string StrongCustomerAuthentication = "Strong Customer Authentication";
            /// <summary>
            /// TFA
            /// </summary>
            public const string TwoFactorAuthentication = "Two Factor Authentication";
        }

        /// <summary>
        /// Grant type.
        /// </summary>
        public static class GrantType
        {
            /// <summary>
            /// Totp custom grant type.
            /// </summary>

            public const string Totp = "totp";
        }
    }
    #endregion
}
