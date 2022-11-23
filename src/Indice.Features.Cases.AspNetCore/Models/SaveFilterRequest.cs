namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// The SaveFilter Request
    /// </summary>
    public class SaveFilterRequest
    {
        /// <summary>
        /// The Name of the request
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The Query Parameters of the request
        /// </summary>
        public string QueryParameters { get; set; }
    }
}