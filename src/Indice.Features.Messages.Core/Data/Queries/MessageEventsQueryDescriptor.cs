using System.Runtime.CompilerServices;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Indice.Features.Messages.Core.Data.Queries;

internal class MessageEventsQueryDescriptor
{
    public MessageEventsQueryDescriptor(DbContext context) {
        switch (context.Database.ProviderName) {
            case "Npgsql.EntityFrameworkCore.PostgreSQL":
                _RollUp = PostgreSqlMessageEventsQueries.RollUp;
                break;
            case "Microsoft.EntityFrameworkCore.SqlServer":
            default:
                _RollUp = SqlServerMessageEventsQueries.RollUp;
                break;
        }
        _scemaName = context.Database.GetService<DatabaseSchemaNameResolver>().GetSchemaName();
    }
    private readonly string _scemaName;
    private readonly string _RollUp;
    public FormattableString RollUp(string type, MessageChannelKind? channelKind = null, DateTimeOffset? rangeStart = null, DateTimeOffset? rangeEnd = null) {
        var Channel = channelKind?.ToString(); 
        var RangeStart = rangeStart;
        var RangeEnd = rangeEnd;
        var Type = type;
        var sql = string.Format(_RollUp, _scemaName)
            .Replace($"@{nameof(Type)}", "{0}")
            .Replace($"@{nameof(Channel)}", "{1}")
            .Replace($"@{nameof(RangeStart)}", "{2}")
            .Replace($"@{nameof(RangeEnd)}", "{3}");
        return FormattableStringFactory.Create(sql, Type, Channel, RangeStart, RangeEnd);
    }
}

internal static class SqlServerMessageEventsQueries
{
    public const string RollUp = @"
SELECT 
    DATEPART(YEAR, [CreatedOn]) AS [Year],
    DATEPART(MONTH, [CreatedOn]) AS [Month],
    DATEPART(Day, [CreatedOn]) AS [Day],
    Count(Id) AS [Events]
FROM 
    [{0}].[MessageEvent] E
where E.[Type] = @Type AND (@Channel IS NULL OR E.[Channel] = @Channel) 
                       AND (@RangeStart IS NULL OR E.[CreatedOn] >= @RangeStart)
                       AND (@RangeEnd IS NULL OR E.[CreatedOn] < @RangeEnd)
GROUP BY 
    ROLLUP(DATEPART(YEAR, [CreatedOn]), DATEPART(MONTH, [CreatedOn]), DATEPART(Day, [CreatedOn]))
ORDER BY 
    [Year], [Month], [Day];";
}

internal static class PostgreSqlMessageEventsQueries
{
    public const string RollUp = @"
SELECT 
    EXTRACT(YEAR FROM ""CreatedOn"") AS ""Year"",
    EXTRACT(MONTH FROM ""CreatedOn"") AS ""Month"",
    EXTRACT(DAY FROM ""CreatedOn"") AS ""Day"",
    Count(""Id"") AS ""Events""
FROM 
    ""{0}"".""MessageEvent"" E
WHERE E.""Type"" = @Type AND (@Channel IS NULL OR E.""Channel"" = @Channel) 
                         AND (@RangeStart IS NULL OR E.""CreatedOn"" >= @RangeStart)
                         AND (@RangeEnd IS NULL OR E.""CreatedOn"" < @RangeEnd)
GROUP BY 
    ROLLUP(EXTRACT(YEAR FROM ""CreatedOn""), EXTRACT(MONTH FROM ""CreatedOn""), EXTRACT(DAY FROM ""CreatedOn""))
ORDER BY 
    ""Year"", ""Month"", ""Day"";";
}