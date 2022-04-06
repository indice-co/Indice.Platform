using Indice.Security;
using Indice.Services;

namespace Indice.Features.Messages.Core
{
    /// <summary>
    /// Constant values for Messages API.
    /// </summary>
    public static class MessagesApi
    {
        /// <summary>
        /// Authentication scheme name used by Messages API.
        /// </summary>
        public const string AuthenticationScheme = "Bearer";
        /// <summary>
        ///  Messages API scope.
        /// </summary>
        public const string Scope = "messages";
        /// <summary>
        /// Default database schema.
        /// </summary>
        public const string DatabaseSchema = "cmp";

        /// <summary>
        ///  Messages API policies.
        /// </summary>
        public static class Policies
        {
            /// <summary>
            /// A user must have the <i>Admin</i> flag or own the <see cref="BasicRoleNames.CampaignManager"/> role.
            /// </summary>
            public const string BeCampaignManager = nameof(BeCampaignManager);
            /// <summary>
            /// A user must have the configured API scope for messages.
            /// </summary>
            public const string HaveMessagesScope = nameof(HaveMessagesScope);
        }
    }

    /// <summary>
    /// Constant values for Messages API queue names.
    /// </summary>
    public class EventNames
    {
        /// <summary>
        /// Name for the event that is raised when a campaign event is published.
        /// </summary>
        public const string CampaignPublished = "campaign-published";
        /// <summary>
        /// Name for the queue that stores events for delivering push notifications.
        /// </summary>
        public const string SendPushNotification = "campaign-send-push-notification";
        /// <summary>
        /// Name for the queue that stores events for resolving contacts from external systems.
        /// </summary>
        public const string ResolveMessage = "campaign-resolve-message";
    }

    /// <summary>
    /// Placeholder for prefixing Messages API endpoints.
    /// </summary>
    public class ApiPrefixes
    {
        /// <summary>
        /// Management API prefix placeholder.
        /// </summary>
        public const string CampaignManagementEndpoints = "[campaignManagementEndpointsPrefix]";
        /// <summary>
        /// Inbox API prefix placeholder.
        /// </summary>
        public const string CampaignInboxEndpoints = "[campaignInboxEndpointsPrefix]";
    }

    /// <summary>
    /// Service keys for Messages.
    /// </summary>
    public class KeyedServiceNames
    {
        /// <summary>
        /// Key service name for <see cref="IPushNotificationService"/> implementation.
        /// </summary>
        public const string PushNotificationServiceKey = "Messages:PushNotificationServiceKey";
        /// <summary>
        /// Key service name for <see cref="IFileService"/> implementation.
        /// </summary>
        public const string FileServiceKey = "Messages:FileServiceKey";
        /// <summary>
        /// Key service name for <see cref="IEventDispatcher"/> implementation.
        /// </summary>
        public const string EventDispatcherServiceKey = "Messages:EventDispatcherServiceKey";
    }
}
