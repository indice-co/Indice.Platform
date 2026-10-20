using System.Security.Claims;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

internal static class AdminDashboardHandlers
{
    private const int MaxIntakeDays = 90;
    private const int MaxStaleAfterDays = 365;

    /// <param name="intakeDays">Number of days, ending today, to report daily intake for (1-90). Defaults to 14.</param>
    /// <param name="staleAfterDays">Open cases older than this many days count as stale (1-365). Defaults to 7.</param>
    /// <param name="adminDashboardService">The dashboard service.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="casesOptions">The cases options.</param>
    public static async Task<Results<Ok<CaseDashboardMetrics>, ValidationProblem>> GetDashboardMetrics(
        IAdminDashboardService adminDashboardService,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions,
        int intakeDays = 14,
        int staleAfterDays = 7) {
        if (intakeDays is < 1 or > MaxIntakeDays) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(intakeDays), $"Must be between 1 and {MaxIntakeDays}."));
        }
        if (staleAfterDays is < 1 or > MaxStaleAfterDays) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(staleAfterDays), $"Must be between 1 and {MaxStaleAfterDays}."));
        }
        return TypedResults.Ok(await adminDashboardService.GetMetrics(currentUser.UserToActor(casesOptions.Value), intakeDays, staleAfterDays));
    }
}
