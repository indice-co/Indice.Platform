namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>Key figures for the back-office dashboard, computed over the cases visible to the current user (drafts excluded).</summary>
public class CaseDashboardMetrics
{
    /// <summary>Number of cases per status of their current checkpoint.</summary>
    public CaseStatusCounts Statuses { get; set; } = new();
    /// <summary>Open cases (<see cref="CaseStatus.Submitted"/> or <see cref="CaseStatus.InProgress"/>) assigned to the current user.</summary>
    public int AssignedToMe { get; set; }
    /// <summary>Open cases that nobody is assigned to.</summary>
    public int Unassigned { get; set; }
    /// <summary>Open cases created more than <see cref="StaleAfterDays"/> days ago.</summary>
    public int Stale { get; set; }
    /// <summary>The age threshold, in days, used for <see cref="Stale"/>.</summary>
    public int StaleAfterDays { get; set; }
    /// <summary>Cases created during the last 7 days, today included.</summary>
    public int NewThisWeek { get; set; }
    /// <summary>Cases created during the 7 days before <see cref="NewThisWeek"/>.</summary>
    public int NewPreviousWeek { get; set; }
    /// <summary>Cases created per day, oldest first, ending today. Days are server calendar days.</summary>
    public List<DailyCaseCount> Intake { get; set; } = [];
    /// <summary>When the metrics were computed.</summary>
    public DateTimeOffset GeneratedAt { get; set; }
}

/// <summary>Case counts per <see cref="CaseStatus"/>.</summary>
public class CaseStatusCounts
{
    /// <summary>Cases waiting to be picked up.</summary>
    public int Submitted { get; set; }
    /// <summary>Cases being worked on.</summary>
    public int InProgress { get; set; }
    /// <summary>Completed cases.</summary>
    public int Completed { get; set; }
    /// <summary>Deleted cases.</summary>
    public int Deleted { get; set; }
    /// <summary>Open cases: <see cref="Submitted"/> plus <see cref="InProgress"/>.</summary>
    public int Open => Submitted + InProgress;
}

/// <summary>Number of cases created on a calendar day.</summary>
public class DailyCaseCount
{
    /// <summary>The calendar day.</summary>
    public DateOnly Date { get; set; }
    /// <summary>Cases created that day.</summary>
    public int Count { get; set; }
}
