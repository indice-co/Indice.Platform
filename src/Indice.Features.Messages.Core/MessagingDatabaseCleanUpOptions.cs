namespace Indice.Features.Messages.Core;

/// <summary>
/// Options for database cleanup operations.
/// </summary>
public class MessagingDatabaseCleanUpOptions
{
    /// <summary>
    /// Gets or sets the retention period for campaigns with inbox (in days). Defaults to <i>180</i>.
    /// </summary>
    public int RetentionDaysForInbox { get; set; } = 180;

    /// <summary>
    /// Gets or sets the retention period for campaigns without inbox (in days). Defaults to <i>120</i>.
    /// </summary>
    public int RetentionDaysForOther { get; set; } = 120;

    /// <summary>
    /// Gets or sets the batch size of records to remove at a time. Defaults to <i>10</i>.
    /// </summary>
    public int DeletionBatchSize { get; set; } = 10;

    /// <summary>
    /// Flag indicating whether the database cleanup process is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;
}
