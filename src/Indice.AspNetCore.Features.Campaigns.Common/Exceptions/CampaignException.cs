namespace Indice.AspNetCore.Features.Campaigns.Exceptions
{
    public class CampaignException : Exception
    {
        public CampaignException() : base() { }

        public CampaignException(string message) : base(message) { }

        public CampaignException(string message, string originOrCode, IEnumerable<string> errors = null) : base(message) {
            Code = originOrCode;
            if (errors != null) {
                Errors.Add(originOrCode, errors.ToArray());
            }
        }

        public Dictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>();
        public string Code { get; set; }

        public static CampaignException CampaignAlreadyPublished(Guid id) => new($"Campaign with id '{id}' is already published.", nameof(id));
        public static CampaignException CampaignNotFound(Guid id) => new($"Campaign with id '{id}' does not exist.", nameof(id));
        public static CampaignException ContactNotFound(Guid id) => new($"Contact with id '{id}' does not exist.", nameof(id));
        public static CampaignException DistributionListNotFound(Guid id) => new($"Distribution list with id '{id}' does not exist.", nameof(id));
        public static CampaignException MessageTypeNotFound(Guid id) => new($"Message type with id '{id}' does not exist.", nameof(id));
        public static CampaignException MessageNotFound(Guid id) => new($"Message with id '{id}' does not exist.", nameof(id));
        public static CampaignException MessageAlreadyRead(Guid id) => new($"Message with id '{id}' is already read.", nameof(id));
        public static CampaignException MessageAlreadyDeleted(Guid id) => new($"Message with id '{id}' is already deleted.", nameof(id));
    }
}
