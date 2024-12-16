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
    private readonly List<string> _translatableProperties = [
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
        options.Converters.Add(new JsonTranslatingConverter(translations));
        return JsonNode.Parse(jsonSource.ToJsonString(options));
    }

    internal class JsonTranslatingConverter : JsonConverter<string>
    {
        public JsonTranslatingConverter(Dictionary<string, string> translations) {
            Translations = translations ?? throw new ArgumentNullException(nameof(translations));
        }

        protected Dictionary<string, string> Translations { get; }

        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var text = reader.GetString();
            if (text is not null && Translations.TryGetValue(text, out var value)) {
                text = value;
            }
            return text;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) {
            if (!Translations.ContainsKey(value)) {
                writer.WriteStringValue(value);
                return;
            }
            writer.WriteStringValue(Translations[value]);
        }
    }
}