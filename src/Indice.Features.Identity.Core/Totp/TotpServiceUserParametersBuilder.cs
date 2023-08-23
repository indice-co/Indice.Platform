using System.Security.Claims;
using System.Text.Json;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Serialization;
using Indice.Services;

namespace Indice.Features.Identity.Core.Totp;

/// <summary>Builder class for configuring <see cref="TotpServiceUser{TUser}"/> parameters.</summary>
/// <typeparam name="TUser">The type of user entity.</typeparam>
public sealed class TotpServiceUserParametersBuilder<TUser> : TotpServiceUserParameters<TUser> where TUser : User
{
    /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.ClaimsPrincipal"/> property.</summary>
    /// <param name="claimsPrincipal">The claims principal.</param>
    public TotpServiceUserMessageBuilder<TUser> ToPrincipal(ClaimsPrincipal claimsPrincipal) {
        ClaimsPrincipal = claimsPrincipal ?? throw new ArgumentNullException($"Parameter {nameof(claimsPrincipal)} cannot be null.");
        return new TotpServiceUserMessageBuilder<TUser>(this);
    }

    /// <summary>Sets the <see cref="User"/> property.</summary>
    /// <param name="user">The user entity.</param>
    public TotpServiceUserMessageBuilder<TUser> ToUser(TUser user) {
        User = user ?? throw new ArgumentNullException($"Parameter {nameof(user)} cannot be null.");
        return new TotpServiceUserMessageBuilder<TUser>(this);
    }

    /// <summary>Creates a new <see cref="TotpServiceUserParameters{TUser}"/> instance.</summary>
    public TotpServiceUserParameters<TUser> Build() => new() {
        ClaimsPrincipal = ClaimsPrincipal,
        Classification = Classification,
        Data = Data,
        DeliveryChannel = DeliveryChannel,
        Message = Message,
        Purpose = Purpose,
        Subject = Subject,
        TokenProvider = TokenProvider,
        User = User,
        AuthenticationMethod = AuthenticationMethod,
        EmailTemplate = EmailTemplate
    };
}

/// <summary>Builder class.</summary>
/// <typeparam name="TUser">The type of user entity.</typeparam>
public sealed class TotpServiceUserMessageBuilder<TUser> where TUser : User
{
    private readonly TotpServiceUserParametersBuilder<TUser> _builder;

    /// <summary>Creates a new instance of <see cref="TotpServiceUserMessageBuilder{TUser}"/>.</summary>
    /// <param name="builder">The instance of <see cref="TotpServiceUserParametersBuilder{TUser}"/>.</param>
    public TotpServiceUserMessageBuilder(TotpServiceUserParametersBuilder<TUser> builder) => _builder = builder;

    /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.Message"/> property.</summary>
    /// <param name="message">The message to be sent in the selected channel. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
    public TotpServiceUserDeliveryChannelBuilder<TUser> WithMessage(string message) {
        _builder.Message = message;
        return new TotpServiceUserDeliveryChannelBuilder<TUser>(_builder);
    }
}

/// <summary>Builder class.</summary>
/// <typeparam name="TUser">The type of user entity.</typeparam>
public sealed class TotpServiceUserDeliveryChannelBuilder<TUser> where TUser : User
{
    private readonly TotpServiceUserParametersBuilder<TUser> _builder;

    /// <summary>Creates a new instance of <see cref="TotpServiceUserDeliveryChannelBuilder{TUser}"/>.</summary>
    /// <param name="builder">The instance of <see cref="TotpServiceUserParametersBuilder{TUser}"/>.</param>
    public TotpServiceUserDeliveryChannelBuilder(TotpServiceUserParametersBuilder<TUser> builder) => _builder = builder;

    /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.DeliveryChannel"/> property.</summary>
    /// <param name="deliveryChannel">Chosen delivery channel.</param>
    public TotpServiceUserOptionalParametersBuilder<TUser> UsingDeliveryChannel(TotpDeliveryChannel deliveryChannel) {
        _builder.DeliveryChannel = deliveryChannel;
        return new TotpServiceUserOptionalParametersBuilder<TUser>(_builder);
    }

    /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.DeliveryChannel"/> property.</summary>
    public TotpServiceUserSmsOrViberOptionalParametersBuilder<TUser> UsingSms() {
        _builder.DeliveryChannel = TotpDeliveryChannel.Sms;
        return new TotpServiceUserSmsOrViberOptionalParametersBuilder<TUser>(_builder);
    }

    /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.DeliveryChannel"/> property.</summary>
    public TotpServiceUserSmsOrViberOptionalParametersBuilder<TUser> UsingViber() {
        _builder.DeliveryChannel = TotpDeliveryChannel.Viber;
        return new TotpServiceUserSmsOrViberOptionalParametersBuilder<TUser>(_builder);
    }

    /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.DeliveryChannel"/> property.</summary>
    public TotpServiceUserOptionalParametersBuilder<TUser> UsingPushNotification() {
        _builder.DeliveryChannel = TotpDeliveryChannel.PushNotification;
        return new TotpServiceUserOptionalParametersBuilder<TUser>(_builder);
    }

    /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.DeliveryChannel"/> property.</summary>
    /// <param name="template">The name of the template to be used.</param>
    public TotpServiceUserOptionalParametersBuilder<TUser> UsingEmail(string template = null) {
        _builder.DeliveryChannel = TotpDeliveryChannel.Email;
        _builder.EmailTemplate = template;
        return new TotpServiceUserOptionalParametersBuilder<TUser>(_builder);
    }
}

/// <summary>Builder class.</summary>
/// <typeparam name="TUser">The type of user entity.</typeparam>
public sealed class TotpServiceUserSmsOrViberOptionalParametersBuilder<TUser> where TUser : User
{
    private readonly TotpServiceUserParametersBuilder<TUser> _builder;

    /// <summary>Creates a new instance of <see cref="TotpServiceUserSmsOrViberOptionalParametersBuilder{TUser}"/>.</summary>
    /// <param name="builder">The instance of <see cref="TotpServiceUserParametersBuilder{TUser}"/>.</param>
    public TotpServiceUserSmsOrViberOptionalParametersBuilder(TotpServiceUserParametersBuilder<TUser> builder) => _builder = builder;

    /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.Purpose"/> property.</summary>
    /// <param name="purpose">The purpose.</param>
    public TotpServiceUserSmsOrViberOptionalParametersBuilder<TUser> WithPurpose(string purpose) {
        _builder.Purpose = purpose;
        return new TotpServiceUserSmsOrViberOptionalParametersBuilder<TUser>(_builder);
    }

    /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.Subject"/> property.</summary>
    /// <param name="subject">The subject of message.</param>
    public TotpServiceUserSmsOrViberOptionalParametersBuilder<TUser> WithSubject(string subject) {
        _builder.Subject = subject;
        return new TotpServiceUserSmsOrViberOptionalParametersBuilder<TUser>(_builder);
    }

    /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.TokenProvider"/> property.</summary>
    /// <param name="tokenProvider">The name of the token provider.</param>
    public TotpServiceUserOptionalParametersBuilder<TUser> UsingTokenProvider(string tokenProvider) {
        _builder.TokenProvider = tokenProvider;
        return new TotpServiceUserOptionalParametersBuilder<TUser>(_builder);
    }
}

/// <summary>Builder class.</summary>
/// <typeparam name="TUser">The type of user entity.</typeparam>
public sealed class TotpServiceUserOptionalParametersBuilder<TUser> where TUser : User
{
    private readonly TotpServiceUserParametersBuilder<TUser> _builder;

    /// <summary>Creates a new instance of <see cref="TotpServiceUserOptionalParametersBuilder{TUser}"/>.</summary>
    /// <param name="builder">The instance of <see cref="TotpServiceUserParametersBuilder{TUser}"/>.</param>
    public TotpServiceUserOptionalParametersBuilder(TotpServiceUserParametersBuilder<TUser> builder) => _builder = builder;

    /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.Purpose"/> property.</summary>
    /// <param name="purpose">The purpose.</param>
    public TotpServiceUserOptionalParametersBuilder<TUser> WithPurpose(string purpose) {
        _builder.Purpose = purpose;
        return new TotpServiceUserOptionalParametersBuilder<TUser>(_builder);
    }

    /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.Classification"/> property.</summary>
    /// <param name="classification">The type of the push notification.</param>
    public TotpServiceUserOptionalParametersBuilder<TUser> WithClassification(string classification) {
        _builder.Classification = classification;
        return new TotpServiceUserOptionalParametersBuilder<TUser>(_builder);
    }

    /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.Subject"/> property.</summary>
    /// <param name="subject">The subject of message.</param>
    public TotpServiceUserOptionalParametersBuilder<TUser> WithSubject(string subject) {
        _builder.Subject = subject;
        return new TotpServiceUserOptionalParametersBuilder<TUser>(_builder);
    }

    /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.Data"/> property.</summary>
    /// <param name="data">The payload data to be sent in push notification.</param>
    public TotpServiceUserOptionalParametersBuilder<TUser> WithData(string data) {
        _builder.Data = data;
        return new TotpServiceUserOptionalParametersBuilder<TUser>(_builder);
    }

    /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.Data"/> property.</summary>
    /// <param name="data">The payload data to be sent in push notification.</param>
    public TotpServiceUserOptionalParametersBuilder<TUser> WithData<TData>(TData data) {
        _builder.Data = JsonSerializer.Serialize(data, JsonSerializerOptionDefaults.GetDefaultSettings());
        return new TotpServiceUserOptionalParametersBuilder<TUser>(_builder);
    }

    /// <summary>Sets the <see cref="TotpServiceUserParameters{TUser}.TokenProvider"/> property.</summary>
    /// <param name="tokenProvider">The name of the token provider.</param>
    public TotpServiceUserOptionalParametersBuilder<TUser> UsingTokenProvider(string tokenProvider) {
        _builder.TokenProvider = tokenProvider;
        return new TotpServiceUserOptionalParametersBuilder<TUser>(_builder);
    }
}

/// <summary>Data class that contains the parameters required for <see cref="TotpServiceUser{TUser}"/>.</summary>
/// <typeparam name="TUser">The type of user entity.</typeparam>
public class TotpServiceUserParameters<TUser> where TUser : User
{
    /// <summary>The claims principal.</summary>
    public ClaimsPrincipal ClaimsPrincipal { get; internal set; }
    /// <summary>The type of the push notification.</summary>
    public string Classification { get; internal set; }
    /// <summary>The payload data to be sent in push notification.</summary>
    public string Data { get; internal set; }
    /// <summary>Chosen delivery channel.</summary>
    public TotpDeliveryChannel DeliveryChannel { get; internal set; }
    /// <summary>The message to be sent in the selected channel. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</summary>
    public string Message { get; internal set; }
    /// <summary>The purpose.</summary>
    public string Purpose { get; internal set; }
    /// <summary>The subject of message.</summary>
    public string Subject { get; internal set; }
    /// <summary>The user entity.</summary>
    public TUser User { get; internal set; }
    /// <summary>The name of the token provider.</summary>
    public string TokenProvider { get; set; }
    /// <summary>The user authentication method to be used.</summary>
    public string AuthenticationMethod { get; set; }
    /// <summary>The email template to be used when <see cref="DeliveryChannel"/> is <see cref="TotpDeliveryChannel.Email"/>.</summary>
    public string EmailTemplate { get; set; }
}
