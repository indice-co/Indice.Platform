using System;
using Indice.Features.GeoIP;
using Indice.Features.Risk.Core.Data.Models;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Indice.Features.Risk.Core.Models;

/// <summary>Describes the risk request model</summary>
public class RiskModel
{
    /// <summary>An amount relative to the event.</summary>
    public decimal? Amount { get; set; }
    /// <summary>The user IP address related to the event occurred.</summary>
    public string? IpAddress { get; set; }
    /// <summary>The unique identifier of the subject performed the event.</summary>
    public string SubjectId { get; set; } = string.Empty;
    /// <summary>The name of the event.</summary>
    public string? Name { get; set; }
    /// <summary>The type of the event.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>The data of the event.</summary>
    public dynamic? Data { get; set; }
    /// <summary>The Id of the source that posted the event.</summary>
    public string? SourceId { get; set; }
    /// <summary>The id of the associated transaction.</summary>
    public string? SourceTransId { get; set; }
    /// <summary>The id of the associated result.</summary>
    public Guid? ResultId { get; set; }
    /// <summary>An optional session identifier the model is associated with.</summary>
    public string? SessionId { get; set; }

    /// <summary>Converts a <see cref="RiskModel"/> to a <see cref="DbRiskEvent"/></summary>
    public DbRiskEvent ToRiskEvent(IPAddressLocator ipAddressLocator) {
        IPAddressLocation? location = null;
        Point? coordinates = null;
        if (System.Net.IPAddress.TryParse(IpAddress, out var ipAddress)) {
            location = ipAddressLocator.GetLocationMetadata(ipAddress);
        }
        if (location?.Coordinates is not null) {
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            coordinates = geometryFactory.CreatePoint(new Coordinate(location.Coordinates.Longitude, location.Coordinates.Latitude));
        }
        return new() {
            Amount = Amount,
            CreatedAt = DateTimeOffset.UtcNow,
            Id = Guid.NewGuid(),
            IpAddress = IpAddress,
            Name = Name,
            SubjectId = SubjectId,
            Type = Type,
            Data = Data,
            SourceId = SourceId,
            SourceTransId = SourceTransId,
            SessionId = SessionId,
            Location = location?.ToString(),
            CountryIsoCode = location?.CountryIsoCode,
            Coordinates = coordinates,
        };
    }
}
