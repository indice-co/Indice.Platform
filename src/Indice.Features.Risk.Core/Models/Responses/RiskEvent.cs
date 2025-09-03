using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Types;

namespace Indice.Features.Risk.Core.Models.Responses;

/// <summary>
/// DTO representing a risk event.
/// </summary>
public class RiskEvent
{
    /// <summary>The unique id of the event.</summary>
    public Guid Id { get; internal set; }
    /// <summary>An amount relative to the event.</summary>
    public decimal? Amount { get; set; }
    /// <summary>The user IP address related to the event occurred.</summary>
    public string? IpAddress { get; set; }
    /// <summary>The unique identifier of the subject performed the event.</summary>
    public string SubjectId { get; set; } = string.Empty;
    /// <summary>Timestamp regarding event creation.</summary>
    public DateTimeOffset CreatedAt { get; set; }
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
    /// <summary>The estimated client location based on the <see cref="IpAddress"/>.</summary>
    public string? Location { get; set; }
    /// <summary>An optional session identifier the event is associated with.</summary>
    public string? SessionId { get; set; }
    /// <summary>Two letter ISO code for the country.</summary>
    public string? CountryIsoCode { get; set; }
    /// <summary>The approximate location of the operation.</summary>
    public GeoPoint? Coordinates { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="RiskEvent"/> from a <see cref="DbRiskEvent"/>.
    /// </summary>
    /// <param name="model">The data model.</param>
    /// <returns></returns>
    public static RiskEvent FromDataModel(DbRiskEvent model) => new() {
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
}
