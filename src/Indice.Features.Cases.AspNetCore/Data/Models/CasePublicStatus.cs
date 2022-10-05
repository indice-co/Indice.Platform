namespace Indice.Features.Cases.Data.Models
{
    /// <summary>
    /// Define the status for the customer. It is defined at the <see cref="DbCheckpointType.PublicStatus"/>.
    /// </summary>
    public enum CasePublicStatus : short
    {
        Submitted,
        InProgress,
        Completed,
        Deleted
    }
}