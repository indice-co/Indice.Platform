using System;

namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// A custom-made that a Back-office user can create, delete etc.
    /// </summary>
    public class Filter
    {
        /// <summary>
        /// The Id of the Filter.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The Name of the Filter.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The Query Parameters of the Filter.
        /// </summary>
        public string QueryParameters { get; set; }
    }
}