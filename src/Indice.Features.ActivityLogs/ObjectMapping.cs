using System.Linq.Expressions;
using Indice.Features.ActivityLogs.Data;
using Indice.Features.ActivityLogs.Models;
using Indice.Types;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Indice.Features.ActivityLogs;

internal static class ObjectMapping
{
    // Cache the geometry factory to avoid creating new instances repeatedly
    private static readonly GeometryFactory _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(4326);

    public static Expression<Func<DbActivityLogEntry, ActivityLogEntry>> ToActivityLogEntry = (logEntry) => new() {
        ActionName = logEntry.ActionName,
        HttpMethod = logEntry.HttpMethod,
        RequestPath = logEntry.RequestPath,
        UserAgent = logEntry.UserAgent,
        EventType = logEntry.EventType,
        Category = logEntry.Category,
        ApplicationId = logEntry.ApplicationId,
        ApplicationName = logEntry.ApplicationName,
        Coordinates = logEntry.Coordinates != null ? new GeoPoint(logEntry.Coordinates.Y, logEntry.Coordinates.X, null) : default,
        CountryIsoCode = logEntry.CountryIsoCode,
        CreatedAt = logEntry.CreatedAt,
        Description = logEntry.Description,
        DeviceId = logEntry.DeviceId,
        ExtraData = logEntry.ExtraData,
        Id = logEntry.Id,
        IpAddress = logEntry.IpAddress,
        Location = logEntry.Location,
        RequestId = logEntry.RequestId,
        ResourceId = logEntry.ResourceId,
        ResourceType = logEntry.ResourceType,
        SessionId = logEntry.SessionId,
        Review = logEntry.Review,
        SubjectId = logEntry.SubjectId,
        SubjectName = logEntry.SubjectName,
        SubjectUnknown = logEntry.SubjectId == null,
        Succeeded = logEntry.Succeeded
    };

    public static DbActivityLogEntry ToDbActivityLogEntry(this ActivityLogEntry logEntry) {
        return new() {
            ActionName = logEntry.ActionName,
            HttpMethod = logEntry.HttpMethod,
            RequestPath = logEntry.RequestPath,
            UserAgent = logEntry.UserAgent,
            EventType = logEntry.EventType,
            Category = logEntry.Category,
            ApplicationId = logEntry.ApplicationId,
            ApplicationName = logEntry.ApplicationName,
            Coordinates = logEntry.Coordinates is not null ? _geometryFactory.CreatePoint(new Coordinate(logEntry.Coordinates.Longitude, logEntry.Coordinates.Latitude)) : default,
            CountryIsoCode = logEntry.CountryIsoCode,
            CreatedAt = logEntry.CreatedAt,
            Description = logEntry.Description,
            DeviceId = logEntry.DeviceId,
            ExtraData = logEntry.ExtraData,
            Id = logEntry.Id,
            IpAddress = logEntry.IpAddress,
            Location = logEntry.Location,
            RequestId = logEntry.RequestId,
            ResourceId = logEntry.ResourceId,
            ResourceType = logEntry.ResourceType,
            SessionId = logEntry.SessionId,
            Review = logEntry.Review,
            SubjectId = logEntry.SubjectId,
            SubjectName = logEntry.SubjectName,
            Succeeded = logEntry.Succeeded
        };
    }

    public static List<DbActivityLogEntry> ToDbActivityLogEntries(this IEnumerable<ActivityLogEntry> logEntries) {
        if (logEntries is ICollection<ActivityLogEntry> collection) {
            var result = new List<DbActivityLogEntry>(collection.Count);
            foreach (var entry in collection) {
                result.Add(entry.ToDbActivityLogEntry());
            }
            return result;
        }
        // Fallback for unknown count
        return logEntries.Select(ToDbActivityLogEntry).ToList();
    }
}
