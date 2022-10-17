using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Features.Kyc.GovGr.Enums;

namespace Indice.Features.Kyc.GovGr.Extensions
{
    /// <summary>
    /// JsonConverter for handling <see cref="GovGrKycStatusCode"/> enums.
    /// </summary>
    public class StringKycStatusCodeConverter : JsonConverter<GovGrKycStatusCode>
    {
        /// <inheritdoc/>
        public override GovGrKycStatusCode Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
                _tryParseEGovKycStatusCode(reader.GetString());

        /// <inheritdoc/>
        public override void Write(
            Utf8JsonWriter writer,
            GovGrKycStatusCode value,
            JsonSerializerOptions options) =>
                writer.WriteStringValue(((int)value).ToString());

        private GovGrKycStatusCode _tryParseEGovKycStatusCode(string stringValue) {
            var result = GovGrKycStatusCode.Unknown;
            // the use of Parse (instead of TryParse) is intentional: we want to get informed in case of a gov.gr response change
            var enumValue = int.Parse(stringValue);
            if (Enum.IsDefined(typeof(GovGrKycStatusCode), enumValue)) {
                result = (GovGrKycStatusCode)enumValue;
            }
            return result;
        }
    }
}