using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Indice.Features.Cases.Core.Services;

/// <inheritdoc />
public class JsonTranslationService : IJsonTranslationService
{
    private readonly string _primaryLanguage;

    /// <summary>The properties where the translate method will search and update their values.</summary>
    private static readonly List<string> _translatableProperties = [
        "title",
        "placeholder",
        "required",
        "pattern",
        "enumNames",
        "helpvalue"
    ];

    /// <summary>Constructs a new <see cref="JsonTranslationService"/>.</summary>
    public JsonTranslationService(IConfiguration configuration) {
        _primaryLanguage = configuration["PrimaryTranslationLanguage"] ?? CasesCoreConstants.DefaultTranslationLanguage;
    }

    /// <inheritdoc />
    public JsonNode? Translate(JsonNode? jsonSource, Dictionary<string, string>? translations, string language) {
        if (jsonSource is null || translations is null) {
            return jsonSource;
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        if (language == _primaryLanguage) {
            return jsonSource;
        }
        if (jsonSource.GetValueKind() == JsonValueKind.String) { // fix for string implicit conversions
            jsonSource = JsonNode.Parse(jsonSource.GetValue<string>())!;
        }
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.GreekExtended, UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
        };
        return JsonNode.Parse(Translate(jsonSource.ToJsonString(options), translations));
    }

    private static string Translate(string jsonSource, Dictionary<string, string> translations) {
        // Create a Json Writer to help us create the new JSON object with the translated values
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        var jsonReaderOptions = new JsonReaderOptions {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };

        // Create a reader to read the json source
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(jsonSource), jsonReaderOptions);

        // Create a buffer to hold the current property name the reader is iterating. Because we're scanning each token
        // when we are getting the property value we have no idea regarding its name, so we need this buffer!
        var propertyNameBuffer = string.Empty;
        while (reader.Read()) {
            switch (reader.TokenType) {
                case JsonTokenType.None:
                    break;
                case JsonTokenType.StartObject:
                    writer.WriteStartObject();
                    break;
                case JsonTokenType.EndObject:
                    writer.WriteEndObject();
                    break;
                case JsonTokenType.StartArray:
                    writer.WriteStartArray();
                    break;
                case JsonTokenType.EndArray:
                    writer.WriteEndArray();
                    break;
                case JsonTokenType.PropertyName:
                    propertyNameBuffer = reader.GetString();
                    writer.WritePropertyName(propertyNameBuffer!);
                    break;
                case JsonTokenType.Comment:
                    break;
                case JsonTokenType.String:
                    var stringValue = reader.GetString() ?? string.Empty;
                    if (_translatableProperties.Contains(propertyNameBuffer!) && translations.ContainsKey(stringValue)) {
                        stringValue = translations[stringValue];
                    }
                    writer.WriteStringValue(stringValue);
                    break;
                case JsonTokenType.Number:
                    writer.WriteRawValue(Encoding.UTF8.GetString(reader.ValueSpan));
                    break;
                case JsonTokenType.True:
                    writer.WriteBooleanValue(true);
                    break;
                case JsonTokenType.False:
                    writer.WriteBooleanValue(false);
                    break;
                case JsonTokenType.Null:
                    writer.WriteNullValue();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}