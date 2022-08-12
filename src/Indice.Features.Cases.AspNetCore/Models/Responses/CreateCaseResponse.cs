using System;

namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// The response payload when creating a case.
    /// </summary>
    public class CreateCaseResponse
    {
        /// <summary>
        /// The Id of the case that created.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The created date of the case that created.
        /// </summary>
        public DateTime Created { get; set; }
    }
}