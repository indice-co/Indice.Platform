using System.Text.Json.Serialization;

namespace Indice.Features.Cases.Core.Models;

/// <summary>Define the case report tag.</summary>
/// <remarks>Any report can be limited to the cases of one channel.</remarks>
[JsonConverter(typeof(JsonStringEnumConverter))] // unfortunately, Elsa uses Newtonsoft.Json and overwrites our Converters...
public enum ReportTag
{
    /// <summary>GroupedByCasetype.</summary>
    GroupedByCasetype,
    /// <summary>Legacy: <see cref="GroupedByCasetype"/> for the Managed channel.</summary>
    AgentGroupedByCasetype,
    /// <summary>Legacy: <see cref="GroupedByCasetype"/> for the External channel.</summary>
    CustomerGroupedByCasetype,
    /// <summary>GroupedByStatus</summary>
    GroupedByStatus,
    /// <summary>Legacy: <see cref="GroupedByStatus"/> for the Managed channel.</summary>
    AgentGroupedByStatus,
    /// <summary>Legacy: <see cref="GroupedByStatus"/> for the External channel.</summary>
    CustomerGroupedByStatus,
    /// <summary>GroupedByGroupId</summary>
    GroupedByGroupId
}