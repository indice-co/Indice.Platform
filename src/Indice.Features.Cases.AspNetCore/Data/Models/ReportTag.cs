using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Indice.Features.Cases.Data.Models
{
    /// <summary>
    /// Define the case report tag.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))] // unfortunately, Elsa uses Newtonsoft.Json and overwrites our Converters...
    public enum ReportTag
    {
        /// <summary>
        /// GroupedByCasetype.
        /// </summary>
        GroupedByCasetype,
        /// <summary>
        /// AgentGroupedByCasetype
        /// </summary>
        AgentGroupedByCasetype,
        /// <summary>
        /// CustomerGroupedByCasetype
        /// </summary>
        CustomerGroupedByCasetype,
        /// <summary>
        /// GroupedByStatus
        /// </summary>
        GroupedByStatus,
        /// <summary>
        /// AgentGroupedByStatus
        /// </summary>
        AgentGroupedByStatus,
        /// <summary>
        /// CustomerGroupedByStatus
        /// </summary>
        CustomerGroupedByStatus,
        /// <summary>
        /// GroupedByGroupId
        /// </summary>
        GroupedByGroupId
    }
}