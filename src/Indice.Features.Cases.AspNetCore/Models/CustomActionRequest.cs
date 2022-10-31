using System;

namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// The custom action trigger request.
    /// </summary>
    public class CustomActionRequest
    {
        /// <summary>
        /// The Id of the custom action.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The value of the custom action (non-required).
        /// </summary>
        public string? Value { get; set; }
    }
}