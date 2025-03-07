// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;

namespace Microsoft.Extensions.Configuration.Json;

/// <summary>Parser/Helper for appsettings json.</summary>
public class JsonConfigurationFileParser
{
    private JsonConfigurationFileParser() { }

    private readonly IDictionary<string, string> _data = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<string> _context = new();
    private string _currentPath = null!;

    /// <summary></summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static IDictionary<string, string> Parse(Stream input) => new JsonConfigurationFileParser().ParseStream(input);

    private IDictionary<string, string> ParseStream(Stream input) {
        _data.Clear();
        var jsonDocumentOptions = new JsonDocumentOptions {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        using (var reader = new StreamReader(input))
        using (var document = JsonDocument.Parse(reader.ReadToEnd(), jsonDocumentOptions)) {
            if (document.RootElement.ValueKind != JsonValueKind.Object) {
                throw new FormatException($"Unsupported JSON token '{document.RootElement.ValueKind}' was found.");
            }
            VisitElement(document.RootElement);
        }
        return _data;
    }

    private void VisitElement(JsonElement element) {
        foreach (var property in element.EnumerateObject()) {
            EnterContext(property.Name);
            VisitValue(property.Value);
            ExitContext();
        }
    }

    private void VisitValue(JsonElement value) {
        switch (value.ValueKind) {
            case JsonValueKind.Object:
                VisitElement(value);
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var arrayElement in value.EnumerateArray()) {
                    EnterContext(index.ToString());
                    VisitValue(arrayElement);
                    ExitContext();
                    index++;
                }
                break;
            case JsonValueKind.Number:
            case JsonValueKind.String:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                var key = _currentPath;
                if (_data.ContainsKey(key)) {
                    throw new FormatException($"A duplicate key '{key}' was found.");
                }
                _data[key] = value.ToString();
                break;
            default:
                throw new FormatException($"Unsupported JSON token '{value.ValueKind}' was found.");
        }
    }

    private void EnterContext(string context) {
        _context.Push(context);
        _currentPath = ConfigurationPath.Combine(_context.Reverse());
    }

    private void ExitContext() {
        _context.Pop();
        _currentPath = ConfigurationPath.Combine(_context.Reverse());
    }
}
