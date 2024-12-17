using System.Security.Claims;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminReportsHandler
{
    public static async Task<Ok<List<GroupByReportResult>>> GetCaseReport(ReportTag reportTag, IAdminReportService adminReportService, ClaimsPrincipal currentUser) =>
        TypedResults.Ok(await adminReportService.GenerateReport(currentUser, reportTag));
}
