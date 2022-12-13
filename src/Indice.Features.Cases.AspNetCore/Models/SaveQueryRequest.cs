namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// The Save Query Request
    /// </summary>
    public class SaveQueryRequest
    {
        /// <summary>
        /// The Friendly Name of the request
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// The Parameters of the request
        /// </summary>
        public string Parameters { get; set; }
    }
}