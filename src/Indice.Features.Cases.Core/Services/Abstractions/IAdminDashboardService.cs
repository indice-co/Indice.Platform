using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>The admin/manage service providing the back-office dashboard figures.</summary>
public interface IAdminDashboardService
{
    /// <summary>Computes the dashboard metrics over the cases visible to the user, using the same visibility rules as the case list.</summary>
    /// <param name="user">The user the metrics are computed for.</param>
    /// <param name="intakeDays">Number of days, ending today, to report daily intake for.</param>
    /// <param name="staleAfterDays">Open cases older than this many days count as stale.</param>
    Task<CaseDashboardMetrics> GetMetrics(UserActor user, int intakeDays, int staleAfterDays);
}
