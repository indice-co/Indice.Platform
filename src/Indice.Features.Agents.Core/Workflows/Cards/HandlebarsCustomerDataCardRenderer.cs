using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using HandlebarsDotNet;
using Indice.Features.Agents.Core.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

namespace Indice.Features.Agents.Core.Workflows.Cards;

/// <summary>
/// Default <see cref="ICustomerDataCardRenderer"/>. Binds the retrieved payload to the Handlebars template
/// <c>{ContentRoot}/Cards/{DataType}.hbs</c> — one template per data type, e.g. <c>ServicePickup.hbs</c> —
/// falling back to <c>Cards/Default.hbs</c> and finally to a built-in generic card. Values are HTML-escaped
/// by Handlebars, so payload content can never inject markup into the surface.
/// </summary>
public sealed class HandlebarsCustomerDataCardRenderer : ICustomerDataCardRenderer
{
    /// <summary>Name of the template used when no data-type specific template exists.</summary>
    public const string DefaultTemplateName = "Default";

    /// <summary>Generic card used when the host ships no template at all.</summary>
    private const string DefaultTemplate = """
        <section class="dex-card" data-card-type="{{dataType}}">
          <header>
            <h3>{{title}}</h3>
            {{#if referenceId}}<p class="dex-muted">{{referenceType}} · {{referenceId}}</p>{{/if}}
          </header>
          <dl>
            {{#each fields}}
            <div><dt>{{label}}</dt><dd>{{value}}</dd></div>
            {{/each}}
          </dl>
        </section>
        """;

    /// <summary>Depth beyond which nested payload members are not flattened onto the card.</summary>
    private const int MaximumDepth = 4;

    private readonly string _baseDirectory;
    private readonly IHandlebars _handlebars = Handlebars.Create();
    private readonly ConcurrentDictionary<string, HandlebarsTemplate<object, object>> _cache = new();

    /// <summary>Creates a new <see cref="HandlebarsCustomerDataCardRenderer"/> reading templates from <c>{ContentRoot}/Cards</c>.</summary>
    public HandlebarsCustomerDataCardRenderer(IHostEnvironment environment) : this(Path.Join(environment.ContentRootPath, "Cards")) { }

    /// <summary>Creates a new <see cref="HandlebarsCustomerDataCardRenderer"/> reading templates from <paramref name="templatesDirectory"/>.</summary>
    public HandlebarsCustomerDataCardRenderer(string templatesDirectory) {
        _baseDirectory = templatesDirectory;
    }

    /// <inheritdoc/>
    public DataContent Render(CustomerDataRecord record) {
        ArgumentNullException.ThrowIfNull(record);
        var model = BuildModel(record);
        var html = Compile(record.DataType)(model);
        return new DataContent(Encoding.UTF8.GetBytes(html), MediaTypeNames.Text.Html) {
            Name = model["title"] as string
        };
    }

    /// <summary>Resolves and caches the compiled template for <paramref name="dataType"/>.</summary>
    private HandlebarsTemplate<object, object> Compile(string dataType) {
        var name = string.IsNullOrWhiteSpace(dataType) ? DefaultTemplateName : dataType;
        return _cache.GetOrAdd(name, key => {
            foreach (var candidate in new[] { key, DefaultTemplateName }) {
                // The template name comes from the external reference type, so keep it to a file name.
                var fileName = Path.GetFileName(candidate);
                var path = Path.Join(_baseDirectory, $"{fileName}.hbs");
                if (File.Exists(path)) {
                    return _handlebars.Compile(File.ReadAllText(path));
                }
            }
            return _handlebars.Compile(DefaultTemplate);
        });
    }

    /// <summary>Projects the payload onto the flat, presentation-friendly model the templates bind against.</summary>
    private static Dictionary<string, object?> BuildModel(CustomerDataRecord record) {
        var fields = new List<Dictionary<string, object?>>();
        Flatten(record.Data, prefix: string.Empty, depth: 0, fields);
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["dataType"] = record.DataType,
            ["referenceId"] = record.Reference.Id,
            ["referenceType"] = record.Reference.Type,
            ["title"] = DeriveTitle(record),
            ["fields"] = fields
        };
    }

    private static string DeriveTitle(CustomerDataRecord record) {
        if (record.Data.ValueKind == JsonValueKind.Object) {
            foreach (var candidate in new[] { "title", "name", "subject", "description" }) {
                if (record.Data.TryGetProperty(candidate, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())) {
                    return value.GetString()!;
                }
            }
        }
        return string.IsNullOrWhiteSpace(record.DataType) ? record.Reference.Id : $"{Humanize(record.DataType)} {record.Reference.Id}".Trim();
    }

    private static void Flatten(JsonElement element, string prefix, int depth, List<Dictionary<string, object?>> fields) {
        if (depth > MaximumDepth) {
            return;
        }
        switch (element.ValueKind) {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject()) {
                    Flatten(property.Value, Combine(prefix, Humanize(property.Name)), depth + 1, fields);
                }
                break;
            case JsonValueKind.Array:
                var index = 1;
                foreach (var item in element.EnumerateArray()) {
                    Flatten(item, Combine(prefix, index.ToString(CultureInfo.InvariantCulture)), depth + 1, fields);
                    index++;
                }
                break;
            case JsonValueKind.Null or JsonValueKind.Undefined:
                break;
            default:
                var text = element.ValueKind switch {
                    JsonValueKind.True => "Yes",
                    JsonValueKind.False => "No",
                    _ => element.ToString()
                };
                if (!string.IsNullOrWhiteSpace(text)) {
                    fields.Add(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
                        ["label"] = string.IsNullOrEmpty(prefix) ? "Value" : prefix,
                        ["value"] = text
                    });
                }
                break;
        }
    }

    private static string Combine(string prefix, string segment) => string.IsNullOrEmpty(prefix) ? segment : $"{prefix} › {segment}";

    /// <summary>Turns <c>licensePlate</c> / <c>license_plate</c> into <c>License plate</c>.</summary>
    private static string Humanize(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            return string.Empty;
        }
        var builder = new StringBuilder(name.Length + 4);
        foreach (var character in name.Replace('_', ' ').Replace('-', ' ')) {
            if (char.IsUpper(character) && builder.Length > 0 && builder[^1] != ' ') {
                builder.Append(' ');
                builder.Append(char.ToLowerInvariant(character));
                continue;
            }
            builder.Append(character);
        }
        var humanized = builder.ToString().Trim();
        return humanized.Length == 0 ? name : char.ToUpperInvariant(humanized[0]) + humanized[1..];
    }
}
