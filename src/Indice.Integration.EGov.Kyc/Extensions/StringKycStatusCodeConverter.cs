using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Integration.EGov.Kyc.Enums;

namespace Indice.Integration.EGov.Kyc.Extensions
{
    /// <summary>
    /// JsonConverter for handling <see cref="KycStatusCode"/> enums.
    /// </summary>
    public class StringKycStatusCodeConverter : JsonConverter<KycStatusCode>
    {
        public override KycStatusCode Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
                _tryParseEGovKycStatusCode(reader.GetString());

        public override void Write(
            Utf8JsonWriter writer,
            KycStatusCode value,
            JsonSerializerOptions options) =>
                writer.WriteStringValue(((int)value).ToString());

        private KycStatusCode _tryParseEGovKycStatusCode(string stringValue) {
            var result = KycStatusCode.Unknown;
            // the use of Parse (instead of TryParse) is intentional: we want to get informed in case of a gov.gr response change
            var enumValue = int.Parse(stringValue);
            if (Enum.IsDefined(typeof(KycStatusCode), enumValue)) {
                result = (KycStatusCode)enumValue;
            }
            return result;
        }
    }
}