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
    }
}
