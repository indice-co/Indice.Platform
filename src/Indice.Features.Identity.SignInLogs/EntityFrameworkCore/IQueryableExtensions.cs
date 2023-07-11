using Indice.Features.Identity.SignInLogs.Data;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.EntityFrameworkCore;
internal static class IQueryableExtensions
{
    public static IQueryable<DbSignInLogEntry> ApplyFilter(this IQueryable<DbSignInLogEntry> query, SignInLogEntryFilter filter) {
        if (filter == null)
            return query;

        if (filter.From.HasValue) {
            query = query.Where(l => l.CreatedAt >= filter.From.Value);
        }
        if (filter.To.HasValue) {
            query = query.Where(l => l.CreatedAt <= filter.To.Value);
        }
        if (filter.Succeeded.HasValue) {
            query = query.Where(l => l.Succeeded == filter.Succeeded.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.SubjectId)) {
            query = query.Where(l => l.SubjectId == filter.SubjectId);
        }
        return query;
    }
}
