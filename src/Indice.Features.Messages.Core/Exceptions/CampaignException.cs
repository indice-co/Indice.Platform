namespace Indice.AspNetCore.Features.Campaigns.Exceptions
{
    /// <summary>
    /// Exception thrown from Campaigns API feature.
    /// </summary>
    public class CampaignException : Exception
    {
        /// <summary>
        /// Creates a new instance of <see cref="CampaignException"/>.
        /// </summary>
        public CampaignException() : base() { }

        /// <summary>
        /// Creates a new instance of <see cref="CampaignException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public CampaignException(string message) : base(message) { }

        /// <summary>
        /// Creates a new instance of <see cref="CampaignException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="code">The error code.</param>
        /// <param name="errors">The errors collection.</param>
        public CampaignException(string message, string code, IEnumerable<string> errors = null) : base(message) {
            Code = code;
            if (errors != null) {
                Errors.Add(code, errors.ToArray());
            }
        }

        /// <summary>
        /// The errors collection.
        /// </summary>
        public Dictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>();
        /// <summary>
        /// The error code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Campaign already published exception.
        /// </summary>
        /// <param name="id">The campaign id.</param>
        public static CampaignException CampaignAlreadyPublished(Guid id) => new($"Campaign with id '{id}' is already published.", nameof(id));
        /// <summary>
        /// Campaign not found exception.
        /// </summary>
        /// <param name="id">The campaign id.</param>
        public static CampaignException CampaignNotFound(Guid id) => new($"Campaign with id '{id}' does not exist.", nameof(id));
        /// <summary>
        /// Contact not found exception.
        /// </summary>
        /// <param name="id">The contact id.</param>
        public static CampaignException ContactNotFound(Guid id) => new($"Contact with id '{id}' does not exist.", nameof(id));
        /// <summary>
        /// Distribution list not found exception.
        /// </summary>
        /// <param name="id">The distribution list id.</param>
        public static CampaignException DistributionListNotFound(Guid id) => new($"Distribution list with id '{id}' does not exist.", nameof(id));
        /// <summary>
        /// Message type not found exception.
        /// </summary>
        /// <param name="id">The message type id.</param>
        public static CampaignException MessageTypeNotFound(Guid id) => new($"Message type with id '{id}' does not exist.", nameof(id));
        /// <summary>
        /// Message not found exception.
        /// </summary>
        /// <param name="id">The message id.</param>
        public static CampaignException MessageNotFound(Guid id) => new($"Message with id '{id}' does not exist.", nameof(id));
        /// <summary>
        /// Message already read exception.
        /// </summary>
        /// <param name="id">The message id.</param>
        public static CampaignException MessageAlreadyRead(Guid id) => new($"Message with id '{id}' is already read.", nameof(id));
        /// <summary>
        /// Message already deleted exception.
        /// </summary>
        /// <param name="id">The message id.</param>
        public static CampaignException MessageAlreadyDeleted(Guid id) => new($"Message with id '{id}' is already deleted.", nameof(id));
    }
}
