using Indice.Features.GeoIP;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Models;
using Indice.Features.Risk.Core.Models.Responses;
using Indice.Types;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using IPAddress = System.Net.IPAddress;

namespace Indice.Features.Risk.Core.Services;

/// <summary>
/// Provides mapping methods for risk models.
/// </summary>
public static class RiskMapper
{
    /// <summary>
    /// Creates a new instance of <see cref="RiskEvent"/> from a <see cref="DbRiskEvent"/>.
    /// </summary>
    /// <param name="model">The data model.</param>
    /// <returns></returns>
    public static RiskEvent EventFromDbModel(DbRiskEvent model) => new() {
        Id = model.Id,
        Amount = model.Amount,
        CreatedAt = model.CreatedAt,
        CountryIsoCode = model.CountryIsoCode,
        Data = model.Data,
        IpAddress = model.IpAddress,
        Location = model.Location,
        Name = model.Name,
        SourceId = model.SourceId,
        SourceTransId = model.SourceTransId,
        SessionId = model.SessionId,
        SubjectId = model.SubjectId,
        Type = model.Type,
        Coordinates = model.Coordinates is not null ? new GeoPoint(latitude: model.Coordinates.Y, longitude: model.Coordinates.X, elevation: model.Coordinates.Z) : null
    };

    /// <summary>Converts a <see cref="RiskModel"/> to a <see cref="DbRiskEvent"/></summary>
    public static DbRiskEvent ToRiskEvent(RiskModel model, IPAddressLocator ipAddressLocator) {
        IPAddressLocation? location = null;
        Point? coordinates = null;
        if (IPAddress.TryParse(model.IpAddress, out var ipAddress)) {
            location = ipAddressLocator.GetLocationMetadata(ipAddress);
        }
        if (location?.Coordinates is not null) {
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            coordinates = geometryFactory.CreatePoint(new Coordinate(location.Coordinates.Longitude, location.Coordinates.Latitude));
        }
        return new() {
            Amount = model.Amount,
            CreatedAt = DateTimeOffset.UtcNow,
            Id = Guid.NewGuid(),
            IpAddress = model.IpAddress,
            Name = model.Name,
            SubjectId = model.SubjectId,
            Type = model.Type,
            Data = model.Data,
            SourceId = model.SourceId,
            SourceTransId = model.SourceTransId,
            SessionId = model.SessionId,
            Location = location?.ToString(),
            CountryIsoCode = location?.CountryIsoCode,
            Coordinates = coordinates,
        };
    }
}
