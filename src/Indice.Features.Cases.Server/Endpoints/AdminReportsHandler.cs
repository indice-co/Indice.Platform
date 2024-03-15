using System.Security.Claims;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminReportsHandler
{
    public static async Task<Results<Ok<List<GroupByReportResult>>, NotFound>> GetCaseReport(IAdminReportService adminReportService, ClaimsPrincipal User, ReportTag reportTag) {
        var result = await adminReportService.GenerateReport(User, reportTag);
        if (result == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(result);
    }
    
}
