namespace Indice.Features.Cases.Models.Requests
{
    /// <summary>
    /// The notification subscription Request.
    /// </summary>
    public class NotificationSubscriptionRequest
    {
        /// <summary>
        /// The Ids of the CaseTypes that the User wants to subscribe to.
        /// </summary>
        public List<Guid> CaseTypeIds { get; set; }
    }
}
