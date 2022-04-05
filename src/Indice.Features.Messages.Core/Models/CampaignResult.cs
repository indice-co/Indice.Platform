using System.Collections.Generic;

namespace Indice.AspNetCore.Features.Campaigns.Models
{
    /// <summary>
    /// Represents the result of a campaign operation.
    /// </summary>
    public sealed class CampaignResult
    {
        /// <summary>
        /// Flag indicating whether if the operation succeeded or not.
        /// </summary>
        public bool Succeeded { get; private set; }
        /// <summary>
        /// An <see cref="IEnumerable{String}"/> containing errors that occurred during the campaign operation.
        /// </summary>
        public IEnumerable<string> Errors { get; private set; } = new List<string>();

        /// <summary>
        /// Creates a success <see cref="CampaignResult"/>.
        /// </summary>
        public static CampaignResult Success() => new() {
            Succeeded = true
        };

        /// <summary>
        /// Creates a failed <see cref="CampaignResult"/>.
        /// </summary>
        /// <param name="errors">A list of errors that occured.</param>
        public static CampaignResult Fail(params string[] errors) => new() {
            Succeeded = false,
            Errors = errors
        };
    }
}
