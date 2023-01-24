namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// The request to update the data of the case.
    /// </summary>
    internal class UpdateCaseRequest
    {
        /// <summary>
        /// The data in json string.
        /// </summary>
        public dynamic Data { get; set; }
    }
}