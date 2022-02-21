using System.Collections.Generic;
using System.Threading.Tasks;
using Indice.Types;

namespace Indice.Services
{
    /// <summary>
    /// Default push notification service implementation for clients who don't support it.
    /// </summary>
    public class PushNotificationServiceNoop : IPushNotificationService
    {
        ///<inheritdoc/>
        public Task Register(string deviceId, string pnsHandle, DevicePlatform devicePlatform, IList<string> tags) => Task.CompletedTask;

        ///<inheritdoc/>
        public Task SendAsync(string title, string body, IList<string> tags, string data = null, string classification = null) => Task.CompletedTask;

        ///<inheritdoc/>
        public Task UnRegister(string deviceId) => Task.CompletedTask;
    }
}
