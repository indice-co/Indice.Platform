namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// The DTO for the CaseTypeSubscriptions for a user.
    /// </summary>
    public class CaseTypeSubscription
    {
        /// <summary>
        /// Indicates if the user is subscribed to the current group.
        /// </summary>
        public bool Subscribed { get; set; }
    }
}