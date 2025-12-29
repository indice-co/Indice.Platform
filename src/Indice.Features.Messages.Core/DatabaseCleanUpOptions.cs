namespace Indice.Features.Messages.Core;

/// <summary>
/// Options for database cleanup operations - retention policies.
/// </summary>
public class DatabaseCleanUpOptions
{
    /// <summary>
    /// Gets or sets the retention period for campaigns with inbox (in days).
    /// </summary>
    public int RetentionDaysForInbox { get; set; } = 180;

    /// <summary>
    /// Gets or sets the retention period for campaigns without inbox (in days).
    /// </summary>
    public int RetentionDaysForOther { get; set; } = 120;

    /// <summary>
    /// Gets or sets the number of records to remove at a time. Defaults to 10.
    /// </summary>
    public int DeletionBatchSize { get; set; } = 10;

    /// <summary>
    /// Flag indicating whether the database cleanup task is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;
}
