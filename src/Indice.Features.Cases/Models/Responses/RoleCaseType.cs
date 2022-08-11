using System;

namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// The role case type model.
    /// </summary>
    internal class RoleCaseType
    {
        /// <summary>
        /// The Id of the role case type.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The user role that relates with this case type.
        /// </summary>
        public string? RoleName { get; set; }

        /// <summary>
        /// The Id of the case type that relates with this role.
        /// </summary>
        public Guid CaseTypeId { get; set; }

        /// <summary>
        /// The Id of the case type that relates with this role.
        /// </summary>
        public Guid CheckpointTypeId { get; set; }

        /// <summary>
        /// The <see cref="CaseType"/> model.
        /// </summary>
        public CaseType CaseType { get; set; }

        /// <summary>
        /// The <see cref="CheckpointType"/> model.
        /// </summary>
        public CheckpointType CheckpointType { get; set; }
    }
}
