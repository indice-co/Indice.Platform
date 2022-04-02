namespace Indice.AspNetCore.Features.Campaigns.Data.Models
{
    /// <summary>
    /// Message type entity.
    /// </summary>
    public class DbMessageType
    {
        /// <summary>
        /// The id of a message type.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        /// <summary>
        /// The name of a message type.
        /// </summary>
        public string Name { get; set; }
    }
}
