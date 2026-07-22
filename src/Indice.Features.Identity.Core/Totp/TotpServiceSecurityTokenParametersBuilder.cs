using Indice.Services;

namespace Indice.Features.Identity.Core.Totp;

/// <summary>Builder class for configuring <see cref="TotpServiceSecurityToken"/> parameters.</summary>
public sealed class TotpServiceSecurityTokenParametersBuilder : TotpServiceSecurityTokenParameters
{
    /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.SecurityToken"/> property.</summary>
    /// <param name="securityToken">A security code. This should be a secret.</param>
    public TotpServiceSecurityTokenMessageBuilder UseSecurityToken(string securityToken) {
        SecurityToken = securityToken ?? throw new ArgumentNullException($"Parameter {nameof(securityToken)} cannot be null.");
        return new TotpServiceSecurityTokenMessageBuilder(this);
    }

/// <summary>Returns the configured <see cref="TotpServiceSecurityTokenParameters"/> instance.</summary>
public TotpServiceSecurityTokenParameters Build() => this;
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
    public TotpServiceSecurityTokenReceiverBuilder WithMessage(string message) {
        _builder.Message = message;
        return new TotpServiceSecurityTokenReceiverBuilder(_builder);
    }
}

/// <summary>Builder class.</summary>
public sealed class TotpServiceSecurityTokenReceiverBuilder
{
    private readonly TotpServiceSecurityTokenParametersBuilder _builder;

    /// <summary>Creates a new instance of <see cref="TotpServiceSecurityTokenReceiverBuilder"/>.</summary>
    /// <param name="builder">The instance of <see cref="TotpServiceSecurityTokenParametersBuilder"/>.</param>
    public TotpServiceSecurityTokenReceiverBuilder(TotpServiceSecurityTokenParametersBuilder builder) => _builder = builder;

    /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.PhoneNumber"/> property.</summary>
    /// <param name="phoneNumber">The receiver's phone number.</param>
    public TotpServiceSecurityTokenDeliveryChannelBuilder ToPhoneNumber(string phoneNumber) {
        _builder.PhoneNumber = phoneNumber;
        return new TotpServiceSecurityTokenDeliveryChannelBuilder(_builder);
    }
    /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.Email"/> property.</summary>
    /// <param name="email">The receiver's email.</param>
    public TotpServiceSecurityTokenDeliveryChannelBuilder ToEmail(string email) {
        _builder.Email = email;
        return new TotpServiceSecurityTokenDeliveryChannelBuilder(_builder);
    }
    /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.UserId"/> property.</summary>
    /// <param name="userId">The receiver's user ID.</param>
    public TotpServiceSecurityTokenDeliveryChannelBuilder ToUser(string userId) {
        _builder.UserId = userId;
        return new TotpServiceSecurityTokenDeliveryChannelBuilder(_builder);
    }
}

/// <summary>Builder class.</summary>
public sealed class TotpServiceSecurityTokenDeliveryChannelBuilder
{
    private readonly TotpServiceSecurityTokenParametersBuilder _builder;

    /// <summary>Creates a new instance of <see cref="TotpServiceSecurityTokenParametersBuilder"/>.</summary>
    /// <param name="builder">The instance of <see cref="TotpServiceSecurityTokenParametersBuilder"/>.</param>
    public TotpServiceSecurityTokenDeliveryChannelBuilder(TotpServiceSecurityTokenParametersBuilder builder) => _builder = builder;

    /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.DeliveryChannel"/> property.</summary>
    public TotpServiceSecurityTokenOptionalParametersBuilder UsingSms() {
        _builder.DeliveryChannel = TotpDeliveryChannel.Sms;
        return new TotpServiceSecurityTokenOptionalParametersBuilder(_builder);
    }

    /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.DeliveryChannel"/> property.</summary>
    public TotpServiceSecurityTokenOptionalParametersBuilder UsingViber() {
        _builder.DeliveryChannel = TotpDeliveryChannel.Viber;
        return new TotpServiceSecurityTokenOptionalParametersBuilder(_builder);
    }
    /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.DeliveryChannel"/> property.</summary>
    public TotpServiceSecurityTokenOptionalParametersBuilder UsingEmail() {
        _builder.DeliveryChannel = TotpDeliveryChannel.Email;
        return new TotpServiceSecurityTokenOptionalParametersBuilder(_builder);
    }
    /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.DeliveryChannel"/> property.</summary>
    public TotpServiceSecurityTokenOptionalParametersBuilder UsingPush() {
        _builder.DeliveryChannel = TotpDeliveryChannel.PushNotification;
        return new TotpServiceSecurityTokenOptionalParametersBuilder(_builder);
    }
    /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.DeliveryChannel"/> property.</summary>
    public TotpServiceSecurityTokenOptionalParametersBuilder UsingChannel(TotpDeliveryChannel channel) {
        _builder.DeliveryChannel = channel;
        return new TotpServiceSecurityTokenOptionalParametersBuilder(_builder);
    }
}

/// <summary>Builder class.</summary>
public sealed class TotpServiceSecurityTokenOptionalParametersBuilder
{
    private readonly TotpServiceSecurityTokenParametersBuilder _builder;

    /// <summary>Creates a new instance of <see cref="TotpServiceSecurityTokenOptionalParametersBuilder"/>.</summary>
    /// <param name="builder">The instance of <see cref="TotpServiceSecurityTokenParametersBuilder"/>.</param>
    public TotpServiceSecurityTokenOptionalParametersBuilder(TotpServiceSecurityTokenParametersBuilder builder) => _builder = builder;

    /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.Purpose"/> property.</summary>
    /// <param name="purpose">The purpose.</param>
    public TotpServiceSecurityTokenOptionalParametersBuilder WithPurpose(string purpose) {
        _builder.Purpose = purpose;
        return new TotpServiceSecurityTokenOptionalParametersBuilder(_builder);
    }

    /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.Subject"/> property.</summary>
    /// <param name="subject">The subject of message.</param>
    public TotpServiceSecurityTokenOptionalParametersBuilder WithSubject(string subject) {
        _builder.Subject = subject;
        return new TotpServiceSecurityTokenOptionalParametersBuilder(_builder);
    }
    /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.Template"/> property.</summary>
    /// <param name="template">The template to use.</param>
    public TotpServiceSecurityTokenOptionalParametersBuilder WithTemplate(string template) {
        _builder.Template = template;
        return new TotpServiceSecurityTokenOptionalParametersBuilder(_builder);
    }

    /// <summary>Sets the <see cref="TotpServiceSecurityTokenParameters.Data"/> property.</summary>
    /// <param name="data">The data.</param>
    public TotpServiceSecurityTokenOptionalParametersBuilder WithData(string data) {
        _builder.Data = data;
        return new TotpServiceSecurityTokenOptionalParametersBuilder(_builder);
    }
}

/// <summary>Data class that contains the parameters required for <see cref="TotpServiceSecurityToken"/>.</summary>
public class TotpServiceSecurityTokenParameters
{
    /// <summary>The type of the push notification.</summary>
    public string? Classification { get; internal set; }
    /// <summary>The payload data to be sent in push notification.</summary>
    public string? Data { get; internal set; }
    /// <summary>The message to be sent in the selected channel. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</summary>
    public string Message { get; internal set; } = null!;
    /// <summary>The purpose.</summary>
    public string Purpose { get; internal set; } = null!;
    /// <summary>A security code. This should be a secret.</summary>
    public string SecurityToken { get; internal set; } = null!;
    /// <summary>The subject of message.</summary>
    public string Subject { get; internal set; } = null!;
    /// <summary>Chosen delivery channel.</summary>
    public TotpDeliveryChannel DeliveryChannel { get; internal set; }
    /// <summary>The user ID.</summary>
    public string? UserId { get; internal set; }
    /// <summary>The receiver's phone number.</summary>
    public string? PhoneNumber { get; internal set; }
    /// <summary>The receiver's email address.</summary>
    public string? Email { get; internal set; }
    /// <summary>The template to use.</summary>
    public string? Template { get; internal set; }
}
