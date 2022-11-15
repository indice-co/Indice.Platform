using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Indice.Features.Cases.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Cases.Services
{
    /// <inheritdoc />
    public class JsonTranslationService : IJsonTranslationService
    {
        private readonly string _primaryLanguage;

        /// <summary>
        /// The properties where the translate method will search and update their values.
        /// </summary>
        private readonly List<string> _translatableProperties = new() {
            "title",
            "placeholder",
            "required",
            "pattern",
            "enumNames",
            "helpvalue"
        };

        /// <summary>
        /// Constructs a new <see cref="JsonTranslationService"/>.
        /// </summary>
        public JsonTranslationService(IConfiguration configuration) {
            _primaryLanguage = configuration.GetSection("PrimaryTranslationLanguage").Value ?? CasesApiConstants.DefaultTranslationLanguage;
        }

        /// <inheritdoc />
        public string Translate(string jsonSource, string jsonTranslations, string language) {
            if (string.IsNullOrEmpty(jsonSource) || string.IsNullOrEmpty(jsonTranslations)) {
                return jsonSource;
            }

            if (string.IsNullOrEmpty(language)) {
                throw new ArgumentNullException(language, nameof(language));
            }

            if (language == _primaryLanguage) {
                return jsonSource;
            }

            var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonTranslations);
            if (translations == null) {
                throw new ArgumentNullException(nameof(translations), "Layout translations cannot be parsed.");
            }

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
            var json = Encoding.UTF8.GetString(stream.ToArray());
            return json;
        }
    }
}