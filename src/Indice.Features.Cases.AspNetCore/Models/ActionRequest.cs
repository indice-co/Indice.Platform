using System;

namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// The request that triggers an action.
    /// </summary>
    public class ActionRequest
    {
        /// <summary>
        /// The Id of the action.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The value of the action (non-required).
        /// </summary>
        public string? Value { get; set; }
    }
}