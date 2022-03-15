using System.Reflection;
using Indice.Security;
using Indice.Services;

namespace Indice.AspNetCore.Features.Campaigns
{
    /// <summary>
    /// Constant values for Campaigns API.
    /// </summary>
    public static class CampaignsApi
    {
        /// <summary>
        /// The assembly name.
        /// </summary>
        public static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        /// <summary>
        /// Authentication scheme name used by Campaigns API.
        /// </summary>
        public const string AuthenticationScheme = "Bearer";
        /// <summary>
        /// Campaigns API scope.
        /// </summary>
        public const string Scope = "campaigns";
        /// <summary>
        /// Default database schema.
        /// </summary>
        public const string DatabaseSchema = "campaign";

        /// <summary>
        /// Campaigns API policies.
        /// </summary>
        public static class Policies 
        {
            /// <summary>
            /// A user must have the <i>Admin</i> flag or own the <see cref="BasicRoleNames.AdminUICampaignsManager"/> role.
            /// </summary>
            public const string BeCampaignsManager = nameof(BeCampaignsManager);
        }
    }

    /// <summary>
    /// Constant values for Campaigns API queue names.
    /// </summary>
    public class QueueNames
    {
        public const string CampaignCreated = "campaign-created";
        public const string SendPushNotification = "campaign-send-push-notification";
    }

    /// <summary>
    /// Service keys for campaigns.
    /// </summary>
    public class KeyedServiceNames
    {
        /// <summary>
        /// Key service name for <see cref="IPushNotificationService"/> implementation.
        /// </summary>
        public const string PushNotificationServiceAzureKey = "Campaigns:PushNotificationServiceAzureKey";
        /// <summary>
        /// Key service name for <see cref="IFileService"/> implementation.
        /// </summary>
        public const string FileServiceKey = "Campaigns:FileServiceKey";
        /// <summary>
        /// Key service name for <see cref="IEventDispatcher"/> implementation.
        /// </summary>
        public const string EventDispatcherAzureServiceKey = "Campaigns:EventDispatcherAzureServiceKey";
    }
}
