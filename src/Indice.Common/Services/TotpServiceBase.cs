using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Services
{
    /// <summary></summary>
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
        /// <param name="message">The message to be sent in the selected channel. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</param>
        /// <param name="subject">The subject of message.</param>
        /// <param name="phoneNumber">Phone number (used when selected <paramref name="channel"/> is <see cref="TotpDeliveryChannel.Sms"/> or <see cref="TotpDeliveryChannel.Viber"/>).</param>
        /// <param name="deviceId">Device identifier (used when selected <paramref name="channel"/> is <see cref="TotpDeliveryChannel.PushNotification"/>).</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="category">The classification type when selected <paramref name="channel"/> is <see cref="TotpDeliveryChannel.PushNotification"/>.</param>
        /// <param name="data">The push notification data (preferably as a JSON string) when selected <paramref name="channel"/> is <see cref="TotpDeliveryChannel.PushNotification"/>.</param>
        /// <exception cref="InvalidOperationException"></exception>
        protected async Task SendToChannelAsync(
            TotpDeliveryChannel channel,
            // TotpMessage
            string message,
            string subject,
            // Recipient
            string phoneNumber = null,
            string deviceId = null,
            string userId = null,
            // TotpMessage
            string category = null,
            string data = null
        ) {
            subject ??= "OTP";
            switch (channel) {
                case TotpDeliveryChannel.Sms:
                case TotpDeliveryChannel.Viber:
                    await ServiceProvider.GetRequiredService<ISmsServiceFactory>()
                                         .Create(channel.ToString())
                                         .SendAsync(phoneNumber, subject, message);
                    break;
                case TotpDeliveryChannel.PushNotification:
                    var pushNotificationService = ServiceProvider.GetRequiredService<IPushNotificationService>();
                    await ServiceProvider.GetRequiredService<IPushNotificationService>()
                                         .SendAsync(builder => {
                                             builder.WithTitle(subject)
                                                    .WithBody(message);
                                             if (string.IsNullOrEmpty(deviceId)) {
                                                 builder.ToDevice(deviceId);
                                             } else {
                                                 if (!string.IsNullOrWhiteSpace(userId)) {
                                                     builder.ToUser(userId);
                                                 }
                                             }
                                             builder.WithData(data)
                                                    .WithClassification(category);
                                         });
                    break;
                case TotpDeliveryChannel.Email:
                case TotpDeliveryChannel.Telephone:
                case TotpDeliveryChannel.EToken:
                    throw new InvalidOperationException($"Delivery channel '{channel}' is not supported.");
                default:
                    break;
            }
        }

        /// <summary></summary>
        /// <param name="cacheKey"></param>
        protected async Task AddCacheKeyAsync(string cacheKey) {
            var unixTime = DateTimeOffset.UtcNow.AddSeconds(CACHE_EXPIRATION_SECONDS).ToUnixTimeSeconds();
            await ServiceProvider.GetRequiredService<IDistributedCache>().SetStringAsync(cacheKey, unixTime.ToString(), new DistributedCacheEntryOptions {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(120)
            });
        }

        /// <summary></summary>
        /// <param name="cacheKey"></param>
        protected async Task<bool> CacheKeyExistsAsync(string cacheKey) {
            var timeText = await ServiceProvider.GetRequiredService<IDistributedCache>().GetStringAsync(cacheKey);
            var exists = timeText != null;
            if (exists && long.TryParse(timeText, out var unixTime)) {
                var time = DateTimeOffset.FromUnixTimeSeconds(unixTime);
                return time >= DateTimeOffset.UtcNow;
            }
            return exists;
        }

        /// <summary></summary>
        /// <param name="purpose"></param>
        /// <param name="phoneNumber"></param>
        protected static string GetModifier(string purpose, string phoneNumber) => $"{purpose}:{phoneNumber}";
    }

    #region Classes supporting TOTP services
    /// <summary><see cref="TotpServiceBase"/> result.</summary>
    public class TotpResult
    {
        /// <summary>Constructs an error result.</summary>
        /// <param name="error">The error.</param>
        public static TotpResult ErrorResult(string error) => new TotpResult {
            Error = error
        };

        /// <summary>Constructs a success result.</summary>
        public static TotpResult SuccessResult => new() { Success = true };
        /// <summary>Indicates success.</summary>
        public bool Success { get; set; }
        /// <summary>The error occurred.</summary>
        public string Error { get; set; }
    }

    /// <summary>Specific exception used to pass errors between <see cref="ITotpService"/> and the caller.</summary>
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

    /// <summary>Constant values for <see cref="ITotpService"/>.</summary>
    public static class TotpConstants
    {
        /// <summary>Token generation purpose.</summary>
        public static class TokenGenerationPurpose
        {
            /// <summary>Strong Customer Authentication.</summary>
            public const string StrongCustomerAuthentication = "Strong Customer Authentication";
            /// <summary>Two Factor Authentication.</summary>
            public const string TwoFactorAuthentication = "Two Factor Authentication";
            /// <summary>Session OTP.</summary>
            public const string SessionOtp = "Session OTP";
        }

        /// <summary>Grant type.</summary>
        public static class GrantType
        {
            /// <summary>TOTP custom grant type.</summary>

            public const string Totp = "totp";
        }
    }
    #endregion
}
