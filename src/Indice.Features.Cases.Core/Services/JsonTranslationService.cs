using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Services.Abstractions;
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

        foreach (var translation in translations) {
            jsonSource[translation.Key]!.ReplaceWith(translation.Value);
        }
        return jsonSource;
    }
}