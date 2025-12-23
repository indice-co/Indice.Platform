namespace Indice.Features.Messages.Core;

/// <summary>
/// Options for database cleanup operations - retention policies.
/// </summary>
public class DatabaseCleanUpOptions
{
    /// <summary>
    /// Gets or sets the retention period for campaigns with inbox (in days).
    /// </summary>
    public int CampaignsWithInboxRetentionPeriodInDays { get; set; } = 120;

    /// <summary>
    /// Gets or sets the retention period for campaigns without inbox (in days).
    /// </summary>
    public int CampaignsWithoutInboxRetentionPeriodInDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the number of records to remove at a time. Defaults to 100.
    /// </summary>
    public int DeletionBatchSize { get; set; } = 10;

    /// <summary>
    /// The cron expression that defines the schedule for the cleanup task.
    /// </summary>
    public string CronExpression { get; set; } = "0 0 2 * * *"; 
}
