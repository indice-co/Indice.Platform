using System;

namespace Indice.AspNetCore.Identity
{
    /// <summary>Builder class for configuring <see cref="TotpServiceSecurityToken"/> parameters.</summary>
    public sealed class TotpServiceSecurityTokenParametersBuilder : TotpServiceSecurityTokenParameters
    {
        /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.SecurityToken"/> property.</summary>
        /// <param name="securityToken">A security code. This should be a secret.</param>
        public TotpServiceSecurityTokenMessageBuilder UseSecurityToken(string securityToken) {
            SecurityToken = securityToken ?? throw new ArgumentNullException($"Parameter {nameof(securityToken)} cannot be null.");
            return new TotpServiceSecurityTokenMessageBuilder(this);
        }

        /// <summary>Creates a new <see cref="TotpServiceUserParameters{TUser}"/> instance.</summary>
        public TotpServiceSecurityTokenParameters Build() => new() {
            Classification = Classification,
            Data = Data,
            Message = Message,
            PhoneNumber = PhoneNumber,
            Purpose = Purpose,
            SecurityToken = SecurityToken,
            Subject = Subject
        };
    }

    /// <summary>Builder class.</summary>
    public sealed class TotpServiceSecurityTokenMessageBuilder
    {
        private readonly TotpServiceSecurityTokenParametersBuilder _builder;

        /// <summary>Creates a new instance of <see cref="TotpServiceSecurityTokenMessageBuilder"/>.</summary>
        /// <param name="builder">The instance of <see cref="TotpServiceSecurityTokenParametersBuilder"/>.</param>
        public TotpServiceSecurityTokenMessageBuilder(TotpServiceSecurityTokenParametersBuilder builder) => _builder = builder;

        /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.Message"/> property.</summary>
        /// <param name="message">The message to be sent in the selected channel. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        public TotpServiceSecurityTokenSubjectBuilder WithMessage(string message) {
            _builder.Subject = message;
            return new TotpServiceSecurityTokenSubjectBuilder(_builder);
        }
    }

    /// <summary>Builder class.</summary>
    public sealed class TotpServiceSecurityTokenSubjectBuilder
    {
        private readonly TotpServiceSecurityTokenParametersBuilder _builder;

        /// <summary>Creates a new instance of <see cref="TotpServiceSecurityTokenSubjectBuilder"/>.</summary>
        /// <param name="builder">The instance of <see cref="TotpServiceSecurityTokenParametersBuilder"/>.</param>
        public TotpServiceSecurityTokenSubjectBuilder(TotpServiceSecurityTokenParametersBuilder builder) => _builder = builder;

        /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.Subject"/> property.</summary>
        /// <param name="subject">The subject of message.</param>
        public TotpServiceSecurityTokenPhoneNumberBuilder WithSubject(string subject) {
            _builder.Subject = subject;
            return new TotpServiceSecurityTokenPhoneNumberBuilder(_builder);
        }
    }

    /// <summary>Builder class.</summary>
    public sealed class TotpServiceSecurityTokenPhoneNumberBuilder
    {
        private readonly TotpServiceSecurityTokenParametersBuilder _builder;

        /// <summary>Creates a new instance of <see cref="TotpServiceSecurityTokenPhoneNumberBuilder"/>.</summary>
        /// <param name="builder">The instance of <see cref="TotpServiceSecurityTokenParametersBuilder"/>.</param>
        public TotpServiceSecurityTokenPhoneNumberBuilder(TotpServiceSecurityTokenParametersBuilder builder) => _builder = builder;

        /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.PhoneNumber"/> property.</summary>
        /// <param name="phoneNumber">The receiver's phone number.</param>
        public TotpServiceSecurityTokenPurposeBuilder ToPhoneNumber(string phoneNumber) {
            _builder.PhoneNumber = phoneNumber;
            return new TotpServiceSecurityTokenPurposeBuilder(_builder);
        }
    }

    /// <summary>Builder class.</summary>
    public sealed class TotpServiceSecurityTokenPurposeBuilder
    {
        private readonly TotpServiceSecurityTokenParametersBuilder _builder;

        /// <summary>Creates a new instance of <see cref="TotpServiceSecurityTokenPurposeBuilder"/>.</summary>
        /// <param name="builder">The instance of <see cref="TotpServiceSecurityTokenParametersBuilder"/>.</param>
        public TotpServiceSecurityTokenPurposeBuilder(TotpServiceSecurityTokenParametersBuilder builder) => _builder = builder;

        /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.Purpose"/> property.</summary>
        /// <param name="purpose">The purpose.</param>
        public void WithPurpose(string purpose) => _builder.Purpose = purpose;
    }

    /// <summary></summary>
    public class TotpServiceSecurityTokenParameters
    {
        /// <summary>The type of the push notification.</summary>
        public string Classification { get; internal set; }
        /// <summary>The payload data to be sent in push notification.</summary>
        public string Data { get; internal set; }
        /// <summary>The message to be sent in the selected channel. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</summary>
        public string Message { get; internal set; }
        /// <summary>The receiver's phone number.</summary>
        public string PhoneNumber { get; set; }
        /// <summary>The purpose.</summary>
        public string Purpose { get; internal set; }
        /// <summary>A security code. This should be a secret.</summary>
        public string SecurityToken { get; set; }
        /// <summary>The subject of message.</summary>
        public string Subject { get; internal set; }
    }
}
