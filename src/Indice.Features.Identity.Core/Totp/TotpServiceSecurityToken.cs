using System.Globalization;
using System.Security;
using System.Text;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.Core.Totp;

/// <summary>Service for sending TOTP codes.</summary>
public sealed class TotpServiceSecurityToken : TotpServiceBase
{
    private readonly Rfc6238AuthenticationService _rfc6238AuthenticationService;
    private readonly IStringLocalizer<TotpServiceSecurityToken> _localizer;
    private readonly ExtendedUserManager<User> _extendedUserManager;

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
        _extendedUserManager = serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
    }

    /// <summary>Creates a TOTP and sends it in the selected <see cref="TotpDeliveryChannel"/>.</summary>
    /// <param name="configureAction">An action to configure the TOTP parameters.</param>
    public Task<TotpResult> SendAsync(Action<TotpServiceSecurityTokenParametersBuilder> configureAction) {
        var builder = new TotpServiceSecurityTokenParametersBuilder();
        configureAction(builder);
        var @params = builder.Build();
        return SendAsync(@params.SecurityToken, @params.Message, @params.Subject,
            @params.PhoneNumber, @params.Email, @params.UserId,
            @params.DeliveryChannel, @params.Purpose, @params.EmailTemplate, @params.Data, @params.Classification);
    }

    /// <summary>Creates a TOTP and sends it in the selected <see cref="TotpDeliveryChannel"/>.</summary>
    /// <param name="securityToken">A security code. This should be a secret.</param>
    /// <param name="message">The message to be sent in the selected channel. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
    /// <param name="subject">The subject of message.</param>
    /// <param name="phoneNumber">The receiver's phone number.</param>
    /// <param name="email">The receiver's email.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="channel">The delivery channel.</param>
    /// <param name="purpose">Optional reason to generate the TOTP.</param>
    /// <param name="emailTemplate">The email template to be used.</param>
    /// <param name="data">Additional data to be included in the message.</param>
    /// <param name="classification">The classification of the message.</param>
    public async Task<TotpResult> SendAsync(
        string securityToken,
        string message,
        string subject,
        string? phoneNumber = null,
        string? email = null,
        string? userId = null,
        TotpDeliveryChannel channel = TotpDeliveryChannel.Sms,
        string? purpose = null,
        string? emailTemplate = null,
        string? data = null,
        string? classification = null
    ) {

        User? resolvedUser = null;
        if (!string.IsNullOrWhiteSpace(userId)) {
            resolvedUser = await _extendedUserManager.FindByIdAsync(userId);
            if (resolvedUser == null) {
                return TotpResult.ErrorResult(_localizer["The specified user does not exist."]);
            }
            phoneNumber = resolvedUser.PhoneNumber;
            email = resolvedUser.Email;
        }
        var result = ValidateChannel(channel, phoneNumber, email, resolvedUser);
        if (!result.IsValid) {
            return TotpResult.ErrorResult(result.ErrorMessage);
        }

        purpose ??= TotpConstants.TokenGenerationPurpose.StrongCustomerAuthentication;
        var recipient = GetRecipient(phoneNumber, email, userId);
        var modifier = GetModifier(purpose, recipient);
        var encodedToken = Encoding.Unicode.GetBytes(securityToken);
        var cacheKey = $"{nameof(TotpServiceSecurityToken)}:{recipient}:{channel}:{purpose}";
        if (await CacheKeyExistsAsync(cacheKey)) {
            return TotpResult.RateLimitedResult(_localizer["Last token has not expired yet. Please wait a few seconds and try again."], await GetCacheKeyExpirationAsync(cacheKey));
        }
        var token = _rfc6238AuthenticationService.GenerateCode(encodedToken, modifier).ToString("D6", CultureInfo.InvariantCulture);
        message = _localizer[message, token];
        await SendToChannelAsync(
            channel,
            new TotpRecipient {
                PhoneNumber = phoneNumber ?? "",
                Email = email ?? "",
                UserId = userId,
            },
            new TotpMessage {
                Message = message,
                Subject = subject,
                EmailTemplate = emailTemplate,
                Data = data,
                Category = classification
            }
        );
        await AddCacheKeyAsync(cacheKey);
        return TotpResult.SuccessResult;
    }
    private string GetRecipient(string? phoneNumber, string? email, string? userId) =>
        !string.IsNullOrWhiteSpace(userId) ? userId :
            !string.IsNullOrWhiteSpace(phoneNumber) ? phoneNumber :
                email ?? throw new SecurityException("No recipient was provided.");

    private (bool IsValid, string ErrorMessage) ValidateChannel(TotpDeliveryChannel channel, string? phoneNumber, string? email, User? user) {
        if (channel is TotpDeliveryChannel.None or TotpDeliveryChannel.Telephone or TotpDeliveryChannel.EToken) {
            return (false, _localizer["Delivery channel '{0}' is not supported.", channel]);
        }
        if (user == null && string.IsNullOrWhiteSpace(phoneNumber) && string.IsNullOrWhiteSpace(email)) {
            return (false, _localizer["No recipient was provided."]);
        }
        if (user == null && channel == TotpDeliveryChannel.PushNotification) {
            return (false, _localizer["User is required for PushNotification channel."]);
        }
        if ((channel == TotpDeliveryChannel.Sms || channel == TotpDeliveryChannel.Viber) && string.IsNullOrWhiteSpace(phoneNumber)) {
            return (false, _localizer["Phone number is required for SMS and Viber channels."]);
        } else if (channel == TotpDeliveryChannel.Email && string.IsNullOrWhiteSpace(email)) {
            return (false, _localizer["Email is required for Email channel."]);
        }
        return (true, string.Empty);
    }


    /// <summary>Verifies the TOTP received for the given user.</summary>
    /// <param name="securityToken">A security code. This should be a secret.</param>
    /// <param name="phoneNumber">The receiver's phone number.</param>
    /// <param name="code">The TOTP code to verify.</param>
    /// <param name="purpose">Optional reason to generate the TOTP.</param>
    /// <param name="email">The receiver's email.</param>
    /// <param name="userId">The user ID.</param>
    public Task<TotpResult> VerifyAsync(
        string securityToken,
        string? phoneNumber,
        string? email,
        string? userId,
        string code,
        string? purpose = null
    ) {
        purpose ??= TotpConstants.TokenGenerationPurpose.StrongCustomerAuthentication;
        if (!int.TryParse(code, out var codeInt)) {
            return Task.FromResult(TotpResult.InvalidFormatResult(_localizer["Totp must be an integer value."]));
        }
        var recipient = GetRecipient(phoneNumber, email, userId);
        var modifier = GetModifier(purpose, recipient);
        var encodedToken = Encoding.Unicode.GetBytes(securityToken);
        var isValidTotp = _rfc6238AuthenticationService.ValidateCode(encodedToken, codeInt, modifier);
        return Task.FromResult(isValidTotp ? TotpResult.SuccessResult : TotpResult.InvalidCodeResult(_localizer["The verification code is invalid."]));
    }
}
