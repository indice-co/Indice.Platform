namespace Indice.Features.Messages.Core.Models.Requests
{
    /// <summary>
    /// The request model used to create a new campaign type.
    /// </summary>
    public class UpsertMessageTypeRequest
    {
        /// <summary>
        /// The name of a campaign type.
        /// </summary>
        public string Name { get; set; }
    }
}
