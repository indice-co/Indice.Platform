using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Indice.Features.Messages.Core.Data;

/// <summary>
/// Appends <c>OPTION (RECOMPILE, USE HINT('DISABLE_OPTIMIZER_ROWGOAL'))</c> to SQL Server queries tagged with <see cref="Tag"/>.
/// </summary>
/// <remarks>
/// Used for the user inbox, whose best plan depends heavily on the recipient (no messages versus thousands of them).
/// Without the hints SQL Server either reuses a plan cached for a very different recipient, or, because of the paging row goal,
/// scans the whole campaign table hoping to find matching rows early, which is very slow for recipients with few or no messages.
/// <para>
/// Other providers run the query unchanged; the tag is only a comment. PostgreSQL does not support query hints, and does not need them here:
/// it plans each execution for the actual parameter values (custom plans), and its generic plan, used when Npgsql auto-prepare is enabled,
/// starts from the recipient's messages as well.
/// </para>
/// </remarks>
internal sealed class QueryHintsInterceptor : DbCommandInterceptor
{
    /// <summary>The query tag that marks a query for the hints.</summary>
    public const string Tag = "Indice.Features.Messages: query hints";

    private const string TagComment = "-- " + Tag;
    private const string Hints = "OPTION (RECOMPILE, USE HINT('DISABLE_OPTIMIZER_ROWGOAL'))";

    public static QueryHintsInterceptor Instance { get; } = new();

    private QueryHintsInterceptor() { }

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result) {
        ApplyHints(command, eventData);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) {
        ApplyHints(command, eventData);
        return ValueTask.FromResult(result);
    }

    private static void ApplyHints(DbCommand command, CommandEventData eventData) {
        if (!command.CommandText.StartsWith(TagComment, StringComparison.Ordinal) || eventData.Context?.Database.IsSqlServer() != true) {
            return;
        }
        command.CommandText = command.CommandText.TrimEnd().TrimEnd(';') + Environment.NewLine + Hints;
    }
}