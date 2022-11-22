namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// A success response message.
    /// </summary>
    public class SuccessMessage
    {
        /// <summary>
        /// The message's Title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The message's Body.
        /// </summary>
        public string? Body { get; set; }
    }
}