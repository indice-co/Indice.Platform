using System.Runtime.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Services;

/// <summary>Base abstract class for creating a TOTP service.</summary>
public abstract class TotpServiceBase
{
    private const int CACHE_EXPIRATION_SECONDS = 30;

    /// <summary>Creates a new instance of <see cref="TotpServiceBase"/>.</summary>
    /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public TotpServiceBase(IServiceProvider serviceProvider) {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</summary>
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>Sends the provided message based on the selected <see cref="TotpDeliveryChannel"/>.</summary>
    /// <param name="channel">The delivery channel.</param>
    /// <param name="totpRecipient">TOTP recipient info DTO.</param>
    /// <param name="totpMessage">TOTP message info DTO.</param>
    /// <exception cref="InvalidOperationException"></exception>
    protected async Task SendToChannelAsync(
        TotpDeliveryChannel channel,
        TotpRecipient totpRecipient,
        TotpMessage totpMessage
    ) {
        switch (channel) {
            case TotpDeliveryChannel.Sms:
            case TotpDeliveryChannel.Viber:
                await ServiceProvider.GetRequiredService<ISmsServiceFactory>()
                                     .Create(channel.ToString())
                                     .SendAsync(totpRecipient.PhoneNumber, totpMessage.Subject, totpMessage.Message);
                break;
            case TotpDeliveryChannel.PushNotification:
                await ServiceProvider.GetRequiredService<IPushNotificationService>()
                                     .SendAsync(builder => {
                                         builder.WithBody(totpMessage.Message)
                                                .WithTitle(totpMessage.Subject ?? "OTP");
                                         if (!string.IsNullOrEmpty(totpRecipient.DeviceId)) {
                                             builder.ToDevice(totpRecipient.DeviceId);
                                         } else {
                                             if (!string.IsNullOrWhiteSpace(totpRecipient.UserId)) {
                                                 builder.ToUser(totpRecipient.UserId);
                                             }
                                         }
                                         builder.WithData(totpMessage.Data)
                                                .WithClassification(totpMessage.Category);
                                     });
                break;
            case TotpDeliveryChannel.Email:
                await ServiceProvider.GetRequiredService<IEmailService>()
                                     .SendAsync(builder => {
                                         builder.To(totpRecipient.Email)
                                                .WithSubject(totpMessage.Subject);
                                         if (string.IsNullOrEmpty(totpMessage.EmailTemplate)) {
                                             builder.WithBody(totpMessage.Message);
                                         } else {
                                             builder.UsingTemplate(totpMessage.EmailTemplate)
                                                    .WithData(new TotpEmail(totpRecipient, totpMessage)); // TODO: check this out could be a problem with Razor Templates. Would prefer dynamic object or expando
                                         }
                                     });
                break;
            case TotpDeliveryChannel.Telephone:
            case TotpDeliveryChannel.EToken:
                throw new InvalidOperationException($"Delivery channel '{channel}' is not supported.");
            default:
                break;
        }
    }

    /// <summary>Adds a cache entry with the given key.</summary>
    /// <param name="cacheKey">The key to the cache.</param>
    protected async Task AddCacheKeyAsync(string cacheKey) {
        var unixTime = DateTimeOffset.UtcNow.AddSeconds(CACHE_EXPIRATION_SECONDS).ToUnixTimeSeconds();
        await ServiceProvider.GetRequiredService<IDistributedCache>().SetStringAsync(cacheKey, unixTime.ToString(), new DistributedCacheEntryOptions {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(120)
        });
    }

    /// <summary>Checks if the given key exists in the cache.</summary>
    /// <param name="cacheKey">The key to the cache.</param>
    protected async Task<bool> CacheKeyExistsAsync(string cacheKey) {
        var timeText = await ServiceProvider.GetRequiredService<IDistributedCache>().GetStringAsync(cacheKey);
        var exists = timeText != null;
        if (exists && long.TryParse(timeText, out var unixTime)) {
            var time = DateTimeOffset.FromUnixTimeSeconds(unixTime);
            return time >= DateTimeOffset.UtcNow;
        }
        return exists;
    }

    /// <summary>Gets a modifier for the TOTP.</summary>
    /// <param name="purpose">The purpose.</param>
    /// <param name="phoneNumber">The phone number.</param>
    protected static string GetModifier(string purpose, string phoneNumber) => $"{purpose}:{phoneNumber}";
}

/// <summary>Supported TOTP delivery factors.</summary>
public enum TotpDeliveryChannel
{
    /// <summary>SMS</summary>
    Sms = 1,
    /// <summary>Email</summary>
    Email = 2,
    /// <summary>Telephone</summary>
    Telephone = 3,
    /// <summary>Viber</summary>
    Viber = 4,
    /// <summary>E-token</summary>
    EToken = 5,
    /// <summary>Push notification</summary>
    PushNotification = 6,
    /// <summary>None</summary>
    None
}

/// <summary>TOTP message info DTO.</summary>
public class TotpMessage
{
    /// <summary>The message to be sent in the selected channel. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</summary>
    public string Message { get; set; }
    /// <summary>The subject of message.</summary>
    public string Subject { get; set; } = "OTP";
    /// <summary>The classification type when selected delivery channel is <see cref="TotpDeliveryChannel.PushNotification"/>.</summary>
    public string Category { get; set; }
    /// <summary>The data (preferably as a JSON string) when selected delivery channel is <see cref="TotpDeliveryChannel.PushNotification"/>.</summary>
    public string Data { get; set; }
    /// <summary>The email template to be used.</summary>
    public string EmailTemplate { get; set; }
}

/// <summary>TOTP recipient info DTO.</summary>
public class TotpRecipient
{
    /// <summary>Phone number (used when selected delivery channel is <see cref="TotpDeliveryChannel.Sms"/> or <see cref="TotpDeliveryChannel.Viber"/>).</summary>
    public string PhoneNumber { get; set; }
    /// <summary>Device identifier (used when selected delivery channel is <see cref="TotpDeliveryChannel.PushNotification"/>).</summary>
    public string DeviceId { get; set; }
    /// <summary>User identifier.</summary>
    public string UserId { get; set; }
    /// <summary>Email (used when selected delivery channel is <see cref="TotpDeliveryChannel.Email"/>).</summary>
    public string Email { get; set; }
}

/// <summary>TOTP view model for databinding to an email template.</summary>
public class TotpEmail
{
    /// <summary>Creates the DTO.</summary>
    /// <param name="recipient">recipient info</param>
    /// <param name="message">message info</param>
    public TotpEmail(TotpRecipient recipient, TotpMessage message) {
        Recipient = recipient;
        Message = message;
    }

    /// <summary>TOTP recipient info DTO.</summary>
    public TotpRecipient Recipient { get; }

    /// <summary>TOTP message info DTO.</summary>
    public TotpMessage Message { get; }

}

/// <summary>TOTP provider metadata.</summary>
public class TotpProviderMetadata
{
    /// <summary>The provider type.</summary>
    public TotpProviderType Type { get; set; }
    /// <summary>The provider channel.</summary>
    public TotpDeliveryChannel Channel { get; set; }
    /// <summary>The name which is used to register the provider in the list of token providers.</summary>
    public string Name => $"{Channel}";
    /// <summary>The display name.</summary>
    public string DisplayName { get; set; }
    /// <summary>Can generate TOTP. If false this provider can only validate TOTPs.</summary>
    public bool CanGenerate { get; set; }
}

/// <summary>Supported TOTP providers used for MFA.</summary>
public enum TotpProviderType
{
    /// <summary>Phone.</summary>
    Phone = 1,
    /// <summary>E-token.</summary>
    EToken = 3,
    /// <summary>Standard OTP.</summary>
    StandardOtp = 4
}

/// <summary><see cref="TotpServiceBase"/> result.</summary>
public class TotpResult
{
    /// <summary>Constructs an error result.</summary>
    /// <param name="error">The error.</param>
    public static TotpResult ErrorResult(string error) => new() {
        Error = error
    };

    /// <summary>Indicates success.</summary>
    public bool Success { get; private set; }
    /// <summary>The error occurred.</summary>
    public string Error { get; private set; }

    /// <summary>Constructs a success result.</summary>
    public static TotpResult SuccessResult => new() {
        Success = true
    };
}

/// <summary>Specific exception used to pass errors when using .</summary>
[Serializable]
public class TotpServiceException : Exception
{
    /// <summary>Constructs the <see cref="TotpServiceException"/>.</summary>
    public TotpServiceException() { }

    /// <summary>Constructs the <see cref="TotpServiceException"/>.</summary>
    public TotpServiceException(string message) : base(message) { }

    /// <summary>Constructs the <see cref="TotpServiceException"/>.</summary>
    public TotpServiceException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>Constructs the <see cref="TotpServiceException"/>.</summary>
    protected TotpServiceException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
