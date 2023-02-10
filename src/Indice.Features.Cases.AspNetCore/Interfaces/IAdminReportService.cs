using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Models;
using Indice.Types;

namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// The admin/manage service regarding cases reports.
    /// </summary>
    public interface IAdminReportService
    {
        ///// <summary>
        ///// Get cases grouped by status
        ///// </summary>
        ///// <returns></returns>
        //Task<List<GroupByReportResult>> GetCasesGroupedByStatus();
        //Task<List<GroupByReportResult>> GetAgentCasesGroupedByStatus();
        //Task<List<GroupByReportResult>> GetCustomerCasesGroupedByStatus();

        ///// <summary>
        ///// Get cases grouped by casetype
        ///// </summary>
        ///// <returns></returns>
        //Task<List<GroupByReportResult>> GetCasesGroupedByCaseType();
        //Task<List<GroupByReportResult>> GetAgentCasesGroupedByCaseType();
        //Task<List<GroupByReportResult>> GetCustomerCasesGroupedByCaseType();

        ///// <summary>
        ///// Get cases grouped by GroupId
        ///// </summary>
        ///// <returns></returns>
        //Task<List<GroupByReportResult>> GetCasesGroupedByGroupId();
        Task<List<GroupByReportResult>> GenerateReport(ReportTag reportTag);
    }
}
