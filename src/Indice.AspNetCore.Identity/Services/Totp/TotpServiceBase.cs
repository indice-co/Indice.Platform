using System;
using System.Threading.Tasks;
using Indice.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Identity
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
        /// <param name="pushNotificationClassification">The classification type when selected <paramref name="channel"/> is <see cref="TotpDeliveryChannel.PushNotification"/>.</param>
        /// <param name="pushNotificationData">The push notification data (preferably as a JSON string) when selected <paramref name="channel"/> is <see cref="TotpDeliveryChannel.PushNotification"/>.</param>
        /// <exception cref="InvalidOperationException"></exception>
        protected async Task SendToChannelAsync(
            TotpDeliveryChannel channel,
            string message,
            string subject,
            string phoneNumber = null,
            string deviceId = null,
            string userId = null,
            string pushNotificationClassification = null,
            string pushNotificationData = null
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
                                                 builder.ToUser(userId);
                                             }
                                             builder.WithData(pushNotificationData)
                                                    .WithClassification(pushNotificationClassification);
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
}
