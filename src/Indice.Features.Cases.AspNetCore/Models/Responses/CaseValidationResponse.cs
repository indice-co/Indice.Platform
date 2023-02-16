namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// CaseValidationResponse.
    /// </summary>
    public class CaseValidationResponse
    {
        /// <summary>
        /// Customer Name.
        /// </summary>
        public string CustomerName { get; set; }
        /// <summary>
        /// CreatedByWhen.
        /// </summary>
        public DateTimeOffset? CreatedByWhen { get; set; }
        /// <summary>
        /// Data.
        /// </summary>
        public dynamic Data { get; set; }
    }
}
