namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a role that will be created on the server.
    /// </summary>
    public class CreateRoleRequest
    {
        /// <summary>
        /// The name of the role.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// A description for the role.
        /// </summary>
        public string Description { get; set; }
    }
}
