namespace Indice.Features.Messages.Core;

/// <summary>Options used to configure the Message analytics feature.</summary>
public class AnalyticsOptions
{
    /// <summary>Feature flag that can switch on/off the analytics feature. </summary>
    /// <remarks>If turned off the system stops tracking message events. Defaults to <c>true</c>.</remarks>
    public bool Enabled { get; set; } = true;
    /// <summary>Options for the send statistics (successful sends per day and month).</summary>
    /// <remarks>Statistics are built from the tracked message events, so they have no effect when <see cref="Enabled"/> is <c>false</c>.</remarks>
    public MessageStatsOptions Stats { get; set; } = new();
}

/// <summary>Options used to configure the send statistics.</summary>
public class MessageStatsOptions
{
    /// <summary>Feature flag that can switch on/off the send statistics. Defaults to <c>false</c>.</summary>
    /// <remarks>Switch it on after the statistics table has been created.</remarks>
    public bool Enabled { get; set; } = false;
    /// <summary>The number of deltas held in memory before new ones are dropped. Defaults to 5000.</summary>
    public int ChannelCapacity { get; set; } = 5000;
    /// <summary>The longest time that deltas wait in memory before they are written. Defaults to 10 minutes.</summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromMinutes(10);
    /// <summary>The number of deltas that triggers a write before <see cref="FlushInterval"/> has passed. Defaults to 500.</summary>
    public int FlushBatchSize { get; set; } = 500;
    /// <summary>The number of failed writes in a row after which the sums carried in memory are dropped. Defaults to 30.</summary>
    public int MaxCarriedFlushes { get; set; } = 30;
    /// <summary>The number of complete UTC days that the verification job recounts. Defaults to 2.</summary>
    public int VerifyLookbackDays { get; set; } = 2;
}
