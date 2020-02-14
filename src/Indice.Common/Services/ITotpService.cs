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
        /// <param name="channel">Delivery channel.</param>
        /// <param name="purpose">Optionaly pass the reason to generate the TOTP.</param>
        /// <param name="securityToken">The generated security token to use, if no <paramref name="principal"/> is provided.</param>
        /// <param name="phoneNumber">The phone number to use, if no <paramref name="principal"/> is provided.</param>
        /// <param name="email">The email to use, if no <paramref name="principal"/> is provided.</param>
        /// <exception cref="TotpServiceException">Used to pass errors between service and the caller.</exception>
        Task<TotpResult> Send(ClaimsPrincipal principal, TotpDeliveryChannel channel = TotpDeliveryChannel.Sms, string purpose = null, string securityToken = null, string phoneNumber = null, string email = null);
        /// <summary>
        /// Verify the code received for the given claims principal.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/>.</param>
        /// <param name="code">The TOTP code.</param>
        /// <param name="provider">Optionaly pass the provider to use to verify. Defaults to DefaultPhoneProvider.</param>
        /// <param name="purpose">Optionaly pass the reason used to generate the TOTP.</param>
        /// <param name="securityToken">The generated security token to use, if no <paramref name="principal"/> is provided.</param>
        /// <param name="phoneNumber">The phone number to use, if no <paramref name="principal"/> is provided.</param>
        /// <param name="email">The email to use, if no <paramref name="principal"/> is provided.</param>
        /// <exception cref="TotpServiceException">Used to pass errors between service and the caller.</exception>
        Task<TotpResult> Verify(ClaimsPrincipal principal, string code, TotpProviderType? provider = null, string purpose = null, string securityToken = null, string phoneNumber = null, string email = null);
        /// <summary>
        /// Gets list of available providers for the given claims principal.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <exception cref="TotpServiceException">used to pass errors between service and the caller.</exception>
        Task<Dictionary<string, TotpProviderMetadata>> GetProviders(ClaimsPrincipal user);
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
        /// <param name="channel">Delivery channel.</param>
        /// <param name="reason">Optionaly pass the reason to generate the TOTP.</param>
        /// <exception cref="TotpServiceException">used to pass errors between service and the caller.</exception>
        public static Task<TotpResult> Send(this ITotpService service, string userId, TotpDeliveryChannel channel = TotpDeliveryChannel.Sms, string reason = null) =>
            service.Send(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(BasicClaimTypes.Subject, userId) })), channel, reason);

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
            return service.Send(totpMessage.ClaimsPrincipal, totpMessage.DeliveryChannel, totpMessage.Purpose, totpMessage.SecurityToken, totpMessage.PhoneNumber, totpMessage.Email);
        }

        /// <summary>
        /// Verify the code received for the given user id.
        /// </summary>
        /// <param name="service">The service to use.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="code">The TOTP code.</param>
        /// <param name="provider">Optionaly pass the provider to use to verify. Defaults to DefaultPhoneProvider</param>
        /// <param name="reason">Optionaly pass the reason used to generate the TOTP.</param>
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
        /// Security token.
        /// </summary>
        public string SecurityToken { get; internal set; }
        /// <summary>
        /// Email address.
        /// </summary>
        public string Email { get; internal set; }
        /// <summary>
        /// Phone number.
        /// </summary>
        public string PhoneNumber { get; internal set; }
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
        public ITotpContactBuilder UsePrincipal(ClaimsPrincipal claimsPrincipal) {
            if (claimsPrincipal == null) {
                throw new ArgumentNullException($"Parameter {nameof(claimsPrincipal)} cannot be null.");
            }
            ClaimsPrincipal = claimsPrincipal ?? throw new ArgumentNullException($"Parameter {nameof(claimsPrincipal)} cannot be null.");
            var totpContactBuilder = new TotpContactBuilder(this);
            return totpContactBuilder;
        }

        /// <summary>
        /// Sets the <see cref="SecurityToken"/> property.
        /// </summary>
        /// <param name="securityToken">Security token.</param>
        /// <returns>The <see cref="ITotpContactBuilder"/>.</returns>
        public ITotpContactBuilder UseSecurityToken(string securityToken) {
            if (string.IsNullOrEmpty(securityToken)) {
                throw new ArgumentNullException($"Parameter {nameof(securityToken)} cannot be null or empty.");
            }
            SecurityToken = securityToken;
            var totpContactBuilder = new TotpContactBuilder(this);
            return totpContactBuilder;
        }

        /// <summary>
        /// Builds the <see cref="TotpMessage"/>.
        /// </summary>
        public TotpMessage Build() => new TotpMessage {
            ClaimsPrincipal = ClaimsPrincipal,
            SecurityToken = SecurityToken,
            Email = Email,
            PhoneNumber = PhoneNumber,
            DeliveryChannel = DeliveryChannel,
            Purpose = Purpose
        };
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
        void WithPurpose(string purpose);
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
        public void WithPurpose(string purpose) {
            if (string.IsNullOrEmpty(purpose)) {
                throw new ArgumentNullException($"Parameter {nameof(purpose)} cannot be null or empty.");
            }
            _totpMessageBuilder.Purpose = purpose;
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
    }

    /// <summary>
    /// Builder for <see cref="TotpMessage"/>.
    /// </summary>
    public interface ITotpContactBuilder
    {
        /// <summary>
        /// Sets the <see cref="TotpMessageBuilder.Email"/> property.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <returns></returns>
        ITotpContactBuilder ToEmail(string email);
        /// <summary>
        /// Sets the <see cref="TotpMessageBuilder.PhoneNumber"/> property.
        /// </summary>
        /// <param name="phoneNumber">Phone number.</param>
        /// <returns></returns>
        ITotpPhoneProviderBuilder ToPhoneNumber(string phoneNumber);
    }

    /// <inheritdoc/>
    public class TotpContactBuilder : ITotpContactBuilder
    {
        private readonly TotpMessageBuilder _totpMessageBuilder;

        /// <summary>
        /// Creates a new instance of <see cref="TotpPurposeBuilder"/>.
        /// </summary>
        /// <param name="totpMessageBuilder">The instance of <see cref="TotpMessageBuilder"/>.</param>
        public TotpContactBuilder(TotpMessageBuilder totpMessageBuilder) {
            _totpMessageBuilder = totpMessageBuilder ?? throw new ArgumentNullException(nameof(totpMessageBuilder));
        }

        /// <inheritdoc/>
        public ITotpContactBuilder ToEmail(string email) {
            if (string.IsNullOrEmpty(email)) {
                throw new ArgumentNullException($"Parameter {nameof(email)} cannot be null or empty.");
            }
            _totpMessageBuilder.Email = email;
            return this;
        }

        /// <inheritdoc/>
        public ITotpPhoneProviderBuilder ToPhoneNumber(string phoneNumber) {
            if (string.IsNullOrEmpty(phoneNumber)) {
                throw new ArgumentNullException($"Parameter {nameof(phoneNumber)} cannot be null or empty.");
            }
            _totpMessageBuilder.PhoneNumber = phoneNumber;
            var totpPhoneProviderBuilder = new TotpPhoneProviderBuilder(_totpMessageBuilder);
            return totpPhoneProviderBuilder;
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
        /// <param name="moreErrors">Additional errors.</param>
        public static TotpResult ErrorResult(string error, params string[] moreErrors) {
            var result = new TotpResult();
            result.Errors.Add(error);
            if (moreErrors?.Length > 0) {
                result.Errors.AddRange(moreErrors);
            }
            return result;
        }

        /// <summary>
        /// Constructs a success result.
        /// </summary>
        public static TotpResult SuccessResult => new TotpResult { Success = true };
        /// <summary>
        /// Indicates success.
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// List of errors.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Spesific exception used to pass errors between <see cref="ITotpService"/> and the caller.
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
        EToken = 5
    }

    /// <summary>
    /// Supported TOTP providers used for MFA.
    /// </summary>
    public enum TotpProviderType
    {
        /// <summary>
        /// Phone
        /// </summary>
        Phone = 1,
        /// <summary>
        /// E-token
        /// </summary>
        EToken = 3
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
        /// Phone number.
        /// </summary>
        public string PhoneNumber { get; set; }
        /// <summary>
        /// Email address.
        /// </summary>
        public string Email { get; set; }
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
