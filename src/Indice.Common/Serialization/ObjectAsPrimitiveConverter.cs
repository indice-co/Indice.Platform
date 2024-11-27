using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization;


/// <summary>A custom <see cref="JsonConverter"/> that tries to convert a JSON object value to dynamic CLR hierarchy based on equvalent primitive types and containers such as <strong>Expando</strong> and <strong>List&lt;object&gt;</strong>.</summary>
/// <remarks>
/// Constructs the converter with preferences.
/// </remarks>
/// <param name="floatFormat">Float json type CLR equivalent</param>
/// <param name="unknownNumberFormat">The fallback option for the case of an <strong>Unknown</strong> numeric type.</param>
/// <param name="objectFormat">The contaner format for a JSON object in CLR.</param>
public class ObjectAsPrimitiveConverter(ObjectAsPrimitiveConverter.FloatKind floatFormat, ObjectAsPrimitiveConverter.UnknownNumberKind unknownNumberFormat, ObjectAsPrimitiveConverter.ObjectKind objectFormat) : JsonConverter<object?>
{
    FloatKind FloatFormat { get; init; } = floatFormat;
    UnknownNumberKind UnknownNumberFormat { get; init; } = unknownNumberFormat;
    ObjectKind ObjectFormat { get; init; } = objectFormat;
 
    /// <inheritdoc/>
    public ObjectAsPrimitiveConverter() : this(FloatKind.Double, UnknownNumberKind.Error, ObjectKind.Expando) { }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options) {
        if (value!.GetType() == typeof(object)) {
            writer.WriteStartObject();
            writer.WriteEndObject();
        } else {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }

    /// <inheritdoc/>
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        switch (reader.TokenType) {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.True:
                return true;
            case JsonTokenType.String:
                return reader.GetString()!;
            case JsonTokenType.Number: {
                    if (reader.TryGetInt32(out var i))
                        return i;
                    if (reader.TryGetInt64(out var l))
                        return l;
                    // BigInteger could be added here.
                    if (FloatFormat == FloatKind.Decimal && reader.TryGetDecimal(out var m))
                        return m;
                    else if (FloatFormat == FloatKind.Double && reader.TryGetDouble(out var d))
                        return d;
                    using var doc = JsonDocument.ParseValue(ref reader);
                    if (UnknownNumberFormat == UnknownNumberKind.JsonElement)
                        return doc.RootElement.Clone();
                    throw new JsonException(string.Format("Cannot parse number {0}", doc.RootElement.ToString()));
                }
            case JsonTokenType.StartArray: {
                    var list = new List<object?>();
                    while (reader.Read()) {
                        switch (reader.TokenType) {
                            default:
                                list.Add(Read(ref reader, typeof(object), options));
                                break;
                            case JsonTokenType.EndArray:
                                return list;
                        }
                    }
                    throw new JsonException();
                }
            case JsonTokenType.StartObject:
                var dict = CreateDictionary();
                while (reader.Read()) {
                    switch (reader.TokenType) {
                        case JsonTokenType.EndObject:
                            return dict;
                        case JsonTokenType.PropertyName:
                            var key = reader.GetString()!;
                            reader.Read();
                            dict.Add(key, Read(ref reader, typeof(object), options));
                            break;
                        default:
                            throw new JsonException();
                    }
                }
                throw new JsonException();
            default:
                throw new JsonException(string.Format("Unknown token {0}", reader.TokenType));
        }
    }

    /// <summary>Creates the container in order to populate the json members.</summary>
    /// <returns>The dictionary according to the <see cref="ObjectKind"/> preference.</returns>
    protected virtual IDictionary<string, object?> CreateDictionary() =>
        ObjectFormat == ObjectKind.Expando ? new ExpandoObject() : new Dictionary<string, object?>();


    /// <summary>Float json type CLR equivalent</summary>
    public enum FloatKind
    {
        /// <summary><see cref="double"/></summary>
        Double,
        /// <summary><see cref="decimal"/></summary>
        Decimal,
    }

    /// <summary>The fallback option for the case of an <strong>Unknown</strong> numeric type.</summary>
    public enum UnknownNumberKind
    {
        /// <summary>Throw error</summary>
        Error,
        /// <summary>Fallback to deserializing into a <see cref="System.Text.Json.JsonElement"/></summary>
        JsonElement,
    }

    /// <summary>The contaner format for a JSON object in CLR.</summary>
    public enum ObjectKind
    {
        /// <summary>Will create an Expando object for every JSON object</summary>
        /// <remarks>Use an expando when you need to pass the object to a dynamic rendering engine with data bindings</remarks>
        Expando,
        /// <summary>Will create a <strong>Dictionary&lt;string, object&gt;</strong> for every JSON object</summary>
        /// <remarks>Prefer this option when stronly type manipulation is needed</remarks>
        Dictionary,
    }

}
