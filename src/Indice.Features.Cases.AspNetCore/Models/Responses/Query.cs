namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// A user defined query.
    /// </summary>
    public class Query
    {
        /// <summary>
        /// The Id of the Query.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The Friendly Name of the Query.
        /// </summary>
        public string FriendlyName { get; set; }
        /// <summary>
        /// The Parameters of the Query.
        /// </summary>
        public string Parameters { get; set; }
    }
}