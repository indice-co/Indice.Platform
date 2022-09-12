namespace Indice.Features.Messages.Core
{
    /// <summary>Options used to configure the Messages API feature.</summary>
    public class MessageMultitenancyOptions
    {
        /// <summary>The minimum access level required.</summary>
        public int AccessLevel { get; set; }
    }
}
