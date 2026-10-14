using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Indice.Features.Cases.Core.Services;

/// <summary>Converts a case type layout written for ajsf (Angular JSON Schema Form) into a JSON Forms UI schema.</summary>
/// <remarks>
/// Layout properties without a JSON Forms equivalent (eg <c>condition</c>, <c>htmlClass</c>, <c>onChange</c>, widget settings)
/// are copied unchanged into the element's <c>options</c>, so the client renderers keep today's behaviour.
/// </remarks>
public static partial class LayoutUiSchemaAdapter
{
    /// <summary>The JSON schema draft that the JSON Forms validator (AJV) accepts by default.</summary>
    public const string SupportedSchemaDraft = "http://json-schema.org/draft-07/schema";

    // ajsf properties that map to a dedicated UI schema member. All other properties are copied to "options".
    private static readonly HashSet<string> MappedProperties = ["key", "type", "items", "tabs", "title", "notitle"];
    private static readonly HashSet<string> KnownContainerTypes = ["section", "fieldset", "div", "flex", "conditional", "array"];

    /// <summary>Converts an ajsf layout to a JSON Forms UI schema.</summary>
    /// <param name="layout">
    /// The layout. Either an ajsf layout array, an object whose properties are ajsf layout arrays (one per schema selector,
    /// eg <c>frontend</c> / <c>backoffice</c>) or a layout that is already a JSON Forms UI schema (returned unchanged).
    /// </param>
    /// <param name="dataSchema">The case type JSON schema. Optional; when given, its draft version is checked.</param>
    public static UiSchemaConversionResult Convert(JsonNode? layout, JsonNode? dataSchema = null) {
        var warnings = new List<string>();
        var uiSchema = layout is null ? null : ConvertRoot(layout, warnings);
        var schemaDraftWarning = GetSchemaDraftWarning(dataSchema);
        if (schemaDraftWarning is not null) {
            warnings.Add(schemaDraftWarning);
        }
        return new UiSchemaConversionResult(uiSchema, warnings);
    }

    /// <summary>Returns a warning when the schema declares a draft other than <see cref="SupportedSchemaDraft"/>, otherwise <see langword="null"/>.</summary>
    /// <param name="dataSchema">The case type JSON schema.</param>
    public static string? GetSchemaDraftWarning(JsonNode? dataSchema) {
        var draft = (dataSchema as JsonObject)?["$schema"]?.GetValueKind() == System.Text.Json.JsonValueKind.String
            ? dataSchema!["$schema"]!.GetValue<string>()
            : null;
        if (string.IsNullOrWhiteSpace(draft) || draft.TrimEnd('#').Equals(SupportedSchemaDraft, StringComparison.OrdinalIgnoreCase)) {
            return null;
        }
        return $"$schema: '{draft}' is not accepted by the JSON Forms validator by default (expects '{SupportedSchemaDraft}').";
    }

    private static JsonNode? ConvertRoot(JsonNode layout, List<string> warnings) {
        switch (layout) {
            case JsonArray items:
                return CreateLayout("VerticalLayout", label: null, ConvertItems(items, "$", arrayPrefix: null, warnings), options: null);
            case JsonObject uiSchema when IsUiSchema(uiSchema):
                return uiSchema.DeepClone();
            case JsonObject selectors when selectors.Count > 0 && selectors.All(p => p.Value is JsonArray || p.Value is JsonObject o && IsUiSchema(o)):
                var result = new JsonObject();
                foreach (var (selector, value) in selectors) {
                    result[selector] = ConvertRoot(value!, warnings);
                }
                return result;
            default:
                warnings.Add("$: the layout is neither an ajsf layout array nor a JSON Forms UI schema; no UI schema was generated.");
                return null;
        }
    }

    private static bool IsUiSchema(JsonObject node) =>
        node["type"]?.GetValueKind() == System.Text.Json.JsonValueKind.String && (node["elements"] is JsonArray || node["scope"] is JsonValue);

    private static JsonArray ConvertItems(JsonArray items, string path, string? arrayPrefix, List<string> warnings) {
        var elements = new JsonArray();
        for (var i = 0; i < items.Count; i++) {
            var element = ConvertItem(items[i], $"{path}[{i}]", arrayPrefix, warnings);
            if (element is not null) {
                elements.Add(element);
            }
        }
        return elements;
    }

    private static JsonNode? ConvertItem(JsonNode? item, string path, string? arrayPrefix, List<string> warnings) {
        if (item is JsonValue value && value.TryGetValue<string>(out var keyOnly)) {
            if (keyOnly == "*") {
                warnings.Add($"{path}: '*' (all remaining schema properties) is not supported; the item was skipped.");
                return null;
            }
            return CreateControl(keyOnly, arrayPrefix, label: null, options: null);
        }
        if (item is not JsonObject node) {
            warnings.Add($"{path}: unsupported layout item; the item was skipped.");
            return null;
        }

        var key = GetString(node, "key");
        var type = GetString(node, "type");
        var items = node["items"] as JsonArray;
        var label = GetLabel(node);
        var options = CopyOptions(node);

        if (type == "tabs") {
            var categories = new JsonArray();
            var tabs = node["tabs"] as JsonArray ?? items ?? [];
            for (var i = 0; i < tabs.Count; i++) {
                if (tabs[i] is not JsonObject tab) {
                    warnings.Add($"{path}.tabs[{i}]: unsupported tab; the tab was skipped.");
                    continue;
                }
                var tabItems = tab["items"] as JsonArray ?? [];
                categories.Add(CreateLayout("Category", GetLabel(tab), ConvertItems(tabItems, $"{path}.tabs[{i}].items", arrayPrefix, warnings), CopyOptions(tab)));
            }
            return CreateLayout("Categorization", label, categories, options);
        }
        if (key is not null) {
            if (type is not null && type != "array") {
                options ??= [];
                options["widget"] = type;
            }
            if (items is not null) {
                // Array with an item layout: JSON Forms renders it with the "detail" UI schema, scoped to a single array item.
                options ??= [];
                options["detail"] = CreateLayout("VerticalLayout", label: null, ConvertItems(items, $"{path}.items", $"{key}[].", warnings), options: null);
            }
            return CreateControl(key, arrayPrefix, label, options);
        }
        if (type is "html" or "help") {
            return CreateElement("Html", label, options);
        }
        if (type is "submit" or "button") {
            return CreateElement(type == "submit" ? "Submit" : "Button", label, options);
        }
        if (items is not null) {
            if (type is not null && !KnownContainerTypes.Contains(type)) {
                warnings.Add($"{path}: unknown container type '{type}'; converted to a vertical layout.");
            }
            var layoutType = type == "flex"
                ? (IsColumnFlex(node) ? "VerticalLayout" : "HorizontalLayout")
                : label?.GetValueKind() == System.Text.Json.JsonValueKind.String ? "Group" : "VerticalLayout";
            return CreateLayout(layoutType, label, ConvertItems(items, $"{path}.items", arrayPrefix, warnings), options);
        }
        warnings.Add($"{path}: item has no 'key', no 'items' and an unsupported type '{type}'; the item was skipped.");
        return null;
    }

    private static JsonObject CreateControl(string key, string? arrayPrefix, JsonNode? label, JsonObject? options) {
        var relativeKey = arrayPrefix is not null && key.StartsWith(arrayPrefix, StringComparison.Ordinal) ? key[arrayPrefix.Length..] : key;
        var control = new JsonObject {
            ["type"] = "Control",
            ["scope"] = ToScope(relativeKey)
        };
        if (label is not null) {
            control["label"] = label;
        }
        if (options is not null) {
            control["options"] = options;
        }
        return control;
    }

    private static JsonObject CreateLayout(string type, JsonNode? label, JsonArray elements, JsonObject? options) {
        var layout = new JsonObject { ["type"] = type };
        if (label is not null) {
            layout["label"] = label;
        }
        layout["elements"] = elements;
        if (options is not null) {
            layout["options"] = options;
        }
        return layout;
    }

    private static JsonObject CreateElement(string type, JsonNode? label, JsonObject? options) {
        var element = new JsonObject { ["type"] = type };
        if (label is not null) {
            element["label"] = label;
        }
        if (options is not null) {
            element["options"] = options;
        }
        return element;
    }

    /// <summary>ajsf <c>title</c> becomes the label; <c>notitle: true</c> hides it.</summary>
    private static JsonNode? GetLabel(JsonObject node) {
        if (node["notitle"] is JsonValue notitle && notitle.TryGetValue<bool>(out var hideTitle) && hideTitle) {
            return JsonValue.Create(false);
        }
        return node["title"]?.DeepClone();
    }

    private static JsonObject? CopyOptions(JsonObject node) {
        JsonObject? options = null;
        foreach (var (name, value) in node) {
            if (MappedProperties.Contains(name)) {
                continue;
            }
            options ??= [];
            options[name] = value?.DeepClone();
        }
        return options;
    }

    private static bool IsColumnFlex(JsonObject node) =>
        (GetString(node, "flex-flow") ?? GetString(node, "flex-direction"))?.Contains("column", StringComparison.OrdinalIgnoreCase) == true;

    private static string? GetString(JsonObject node, string property) =>
        node[property] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    /// <summary>Converts an ajsf key (eg <c>a.b</c>, <c>a[].b</c>, <c>/a/0/b</c>) to a JSON Forms scope (eg <c>#/properties/a/items/properties/b</c>).</summary>
    private static string ToScope(string key) {
        var scope = new StringBuilder("#");
        foreach (var segment in key.Split(['.', '/'], StringSplitOptions.RemoveEmptyEntries)) {
            var arrayMatch = ArraySegment().Match(segment);
            if (arrayMatch.Success) {
                scope.Append("/properties/").Append(arrayMatch.Groups["name"].Value).Append("/items");
            } else if (segment == "-" || segment.All(char.IsDigit)) {
                scope.Append("/items");
            } else {
                scope.Append("/properties/").Append(segment);
            }
        }
        return scope.ToString();
    }

    [GeneratedRegex(@"^(?<name>.+)\[\d*\]$")]
    private static partial Regex ArraySegment();
}

/// <summary>The result of <see cref="LayoutUiSchemaAdapter.Convert(JsonNode?, JsonNode?)"/>.</summary>
/// <param name="UiSchema">The JSON Forms UI schema, or <see langword="null"/> when there is no layout or it could not be converted.</param>
/// <param name="Warnings">Layout items that could not be converted, or a schema draft that the client validator does not accept.</param>
public record UiSchemaConversionResult(JsonNode? UiSchema, IReadOnlyList<string> Warnings);
