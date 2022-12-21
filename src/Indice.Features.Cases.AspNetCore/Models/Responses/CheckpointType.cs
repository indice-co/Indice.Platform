namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// The checkpoint model.
    /// </summary>
    internal class CheckpointType
    {
        /// <summary>
        /// The Id of the checkpoint.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The code of the checkpoint.
        /// </summary>
        public string Code { get; set; }
    }
}
