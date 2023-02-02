using Indice.Features.Cases.Models.Responses;

namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// The notification subscription setting.
    /// </summary>
    public class NotificationSubscriptionSetting
    {
        /// <summary>
        /// The notification subscription CaseType.
        /// </summary>
        public CaseTypePartial CaseType { get; set; }
        /// <summary>
        /// Indicates whether the user has subscribed to that particular subscription.
        /// </summary>
        public bool Subscribed { get; set; }
    }
}