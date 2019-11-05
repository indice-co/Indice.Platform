namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Describes the grant type of a client in the database.
    /// </summary>
    public class GrantTypeInfo
    {
        /// <summary>
        /// The id of the grant type in the system.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// The name of the grant type.
        /// </summary>
        public string Name { get; set; }
    }
}
