using System;
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
                    var pushNotificationService = ServiceProvider.GetRequiredService<IPushNotificationService>();
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
    }
}
