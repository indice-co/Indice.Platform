using System.Text.Json.Serialization;

namespace Indice.Features.Cases.Core.Models;

/// <summary>Define the case report tag.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))] // unfortunately, Elsa uses Newtonsoft.Json and overwrites our Converters...
public enum ReportTag
{
    /// <summary>GroupedByCasetype.</summary>
    GroupedByCasetype,
    /// <summary>AgentGroupedByCasetype</summary>
    AgentGroupedByCasetype,
    /// <summary>CustomerGroupedByCasetype</summary>
    CustomerGroupedByCasetype,
    /// <summary>GroupedByStatus</summary>
    GroupedByStatus,
    /// <summary>AgentGroupedByStatus</summary>
    AgentGroupedByStatus,
    /// <summary>CustomerGroupedByStatus</summary>
    CustomerGroupedByStatus,
    /// <summary>GroupedByGroupId</summary>
    GroupedByGroupId
}