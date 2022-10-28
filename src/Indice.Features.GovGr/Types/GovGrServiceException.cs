using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Indice.Features.GovGr.Types
{
    /// <summary>
    /// An exception thrown when a <see cref="GovGrClient"/> operation fails
    /// </summary>
    public class GovGrServiceException : Exception
    {
        /// <inheritdoc/>
        public GovGrServiceException(string message) : this(message, null) {

        }

        /// <inheritdoc/>
        public GovGrServiceException(string message, Exception innerException) : base(message, innerException) {
            if (message?.Length > 1 && message[0] == '{') {
                var json = JsonSerializer.Deserialize<JsonElement>(message);
                var problemDetailsProperty = json.EnumerateObject().FirstOrDefault();
                if (!default(JsonProperty).Equals(problemDetailsProperty)) { 
                    Title = problemDetailsProperty.Name;
                    Code = problemDetailsProperty.Value.TryGetProperty("code", out var codeProp) ? codeProp.GetInt32() : 0;
                    Details = problemDetailsProperty.Value.TryGetProperty("message", out var messageProp) ? messageProp.GetString() : string.Empty;
                }
            }
        }

        /// <summary>
        /// Http response code
        /// </summary>
        public int Code { get; }
        /// <summary>
        /// Http response
        /// </summary>
        public string Title { get; }
        /// <summary>
        /// Http response message
        /// </summary>
        public string Details { get; }
    }
}
