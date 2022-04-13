namespace Indice.Features.Messages.Core.Manager
{
    /// <summary>
    /// Represents the result of a create campaign operation.
    /// </summary>
    public class CreateCampaignResult : CampaignResult
    {
        /// <summary>
        /// The <see cref="Guid"/> of the campaign created.
        /// </summary>
        public Guid CampaignId { get; private set; }

        internal static CreateCampaignResult Success(Guid campaignId) => new() {
            Succeeded = true,
            CampaignId = campaignId
        };

        internal static CreateCampaignResult Fail(params string[] errors) => new() {
            Succeeded = false,
            Errors = errors
        };
    }

    /// <summary>
    /// Represents the result of a campaign operation.
    /// </summary>
    public class CampaignResult
    {
        /// <summary>
        /// Flag indicating whether if the operation succeeded or not.
        /// </summary>
        public bool Succeeded { get; protected set; }
        /// <summary>
        /// An <see cref="IEnumerable{String}"/> containing errors that occurred during the campaign operation.
        /// </summary>
        public IEnumerable<string> Errors { get; protected set; } = new List<string>();
    }
}
