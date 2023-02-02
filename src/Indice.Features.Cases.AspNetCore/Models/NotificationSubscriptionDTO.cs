namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// The notification subscription DTO.
    /// </summary>
    public class NotificationSubscriptionDTO
    {
        /// <summary>
        /// The notification subscription Settings.
        /// </summary>
        public List<NotificationSubscriptionSetting> NotificationSubscriptionSettings { get; set; }
    }
}