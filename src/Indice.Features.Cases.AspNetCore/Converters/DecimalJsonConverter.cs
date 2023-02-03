using Newtonsoft.Json;

namespace Indice.Features.Cases.Converters;

/// <summary>
/// Decimal to json converter for correcting javascript "int" numbers and force them to be int instead of decimal.
/// <remarks>https://stackoverflow.com/a/32726630</remarks>
/// </summary>
internal class DecimalJsonConverter : JsonConverter
{
    /// <inheritdoc />
    public DecimalJsonConverter() {
    }

    public override bool CanRead {
        get {
            return false;
        }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
        throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
    }

    public override bool CanConvert(Type objectType) =>
        objectType == typeof(decimal) || objectType == typeof(float) || objectType == typeof(double);

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
        if (DecimalJsonConverter.IsWholeValue(value)) {
            writer.WriteRawValue(JsonConvert.ToString(Convert.ToInt64(value)));
        } else {
            writer.WriteRawValue(JsonConvert.ToString(value));
        }
    }

    private static bool IsWholeValue(object value) {
        if (value is decimal decimalValue) {
            int precision = (Decimal.GetBits(decimalValue)[3] >> 16) & 0x000000FF;
            return precision == 0;
        } else if (value is float floatValue) {
            return floatValue == Math.Truncate(floatValue);
        } else if (value is double doubleValue) {
            return doubleValue == Math.Truncate(doubleValue);
        }

        return false;
    }
}