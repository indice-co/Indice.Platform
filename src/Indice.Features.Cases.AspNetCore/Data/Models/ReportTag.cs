using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Indice.Features.Cases.Data.Models
{
    /// <summary>
    /// Define the status for the customer. It is defined at the <see cref="DbCheckpointType.Status"/>.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))] // unfortunately, Elsa uses Newtonsoft.Json and overwrites our Converters...
    public enum ReportTag
    {
        /// <summary>
        /// GroupedByCasetype.
        /// </summary>
        GroupedByCasetype,
        /// <summary>
        /// .
        /// </summary>
        AgentGroupedByCasetype,
        /// <summary>
        /// .
        /// </summary>
        CustomerGroupedByCasetype,
        /// <summary>
        /// .
        /// </summary>
        GroupedByStatus,
        /// <summary>
        /// .
        /// </summary>
        AgentGroupedByStatus,
        /// <summary>
        /// .
        /// </summary>
        CustomerGroupedByStatus,
    }
}