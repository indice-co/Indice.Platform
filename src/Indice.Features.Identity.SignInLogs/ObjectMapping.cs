using System.Linq.Expressions;
using Indice.Features.Identity.SignInLogs.Data;
using Indice.Features.Identity.SignInLogs.Models;
using Indice.Types;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Indice.Features.Identity.SignInLogs;

internal static class ObjectMapping
{
    public static Expression<Func<DbSignInLogEntry, SignInLogEntry>> ToSignInLogEntry = (logEntry) => new() {
        ActionName = logEntry.ActionName,
        ApplicationId = logEntry.ApplicationId,
        ApplicationName = logEntry.ApplicationName,
        Coordinates = logEntry.Coordinates != null ? new GeoPoint(logEntry.Coordinates.Y, logEntry.Coordinates.X, null) : default,
        CountryIsoCode = logEntry.CountryIsoCode,
        CreatedAt = logEntry.CreatedAt,
        Description = logEntry.Description,
        DeviceId = logEntry.DeviceId,
        ExtraData = logEntry.ExtraData,
        GrantType = logEntry.GrantType,
        Id = logEntry.Id,
        IpAddress = logEntry.IpAddress,
        Location = logEntry.Location,
        RequestId = logEntry.RequestId,
        ResourceId = logEntry.ResourceId,
        ResourceType = logEntry.ResourceType,
        Review = logEntry.Review,
        SessionId = logEntry.SessionId,
        SignInType = logEntry.SignInType,
        SubjectId = logEntry.SubjectId,
        SubjectName = logEntry.SubjectName,
        Succeeded = logEntry.Succeeded
    };

    public static DbSignInLogEntry ToDbSignInLogEntry(this SignInLogEntry logEntry) {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(4326);
        return new() {
            ActionName = logEntry.ActionName,
            ApplicationId = logEntry.ApplicationId,
            ApplicationName = logEntry.ApplicationName,
            Coordinates = logEntry.Coordinates is not null ? geometryFactory.CreatePoint(new Coordinate(logEntry.Coordinates.Longitude, logEntry.Coordinates.Latitude)) : default,
            CountryIsoCode = logEntry.CountryIsoCode,
            CreatedAt = logEntry.CreatedAt,
            Description = logEntry.Description,
            DeviceId = logEntry.DeviceId,
            ExtraData = logEntry.ExtraData,
            GrantType = logEntry.GrantType,
            Id = logEntry.Id,
            IpAddress = logEntry.IpAddress,
            Location = logEntry.Location,
            RequestId = logEntry.RequestId,
            ResourceId = logEntry.ResourceId,
            ResourceType = logEntry.ResourceType,
            Review = logEntry.Review,
            SessionId = logEntry.SessionId,
            SignInType = logEntry.SignInType,
            SubjectId = logEntry.SubjectId,
            SubjectName = logEntry.SubjectName,
            Succeeded = logEntry.Succeeded
        };
    }
}
