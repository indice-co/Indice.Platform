using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Serialization;
using Json.Patch;
using Json.Pointer;

namespace Indice.Features.Cases.Extensions;

internal static class JsonNodeExtensions
{
    /// <summary>
    /// Should be a JsonArray of JsonObjects containing the keys "op" and "path"
    /// and one of either `value` or `from` 
    /// </summary>
    public static bool IsJsonPatch(this JsonNode jsonNode) {
        if (jsonNode is not JsonArray jsonArray) {
            return false;
        }

        foreach (var node in jsonArray) {
            if (node is not JsonObject jsonObject) {
                return false;
            }
            
            if (jsonObject.Count > 3) {
                return false;
            }
            
            if (!jsonObject.ContainsKey("op") || !jsonObject.ContainsKey("path")) {
                return false;
            }

            if (!jsonObject.ContainsKey("value") && !jsonObject.ContainsKey("from")) {
                return false;
            }
            
        }

        return true;
    }
    
    /// <summary>
    /// Recursively merges two JsonNodes by ensuring that the structure of the `original` node is
    /// updated while preventing overwrites of incompatible JsonNode types.
    /// - If the `toMerge` node contains multiple nested types, each one of them should exist as-is in the `original` node.
    /// - If the `toMerge` node contains nested types that the `original` node only partially matches (subset),
    ///   the remaining nested types are added to the corresponding location in the `original` node.
    /// - For simple JsonObjects this works like add or replace property. 
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if JsonNode types are not the same or if trying to merge JsonValue.
    /// </exception>
    public static JsonNode Merge(this JsonNode original, JsonNode toMerge) {
        if (original == null || toMerge == null) {
            return original;
        }
        
        switch (original) {
            case JsonObject jsonBaseObj when toMerge is JsonObject tempMergeObj: {
                var mergeDictionary = tempMergeObj.ToArray();
                tempMergeObj.Clear();

                foreach (var toMergeNode in mergeDictionary) {
                    switch (jsonBaseObj[toMergeNode.Key]) {
                        
                        // do not allow merging of different JsonNode types to prevent from accidentally overriding data
                        case JsonObject when toMergeNode.Value is not JsonObject:
                            throw new InvalidOperationException("Cannot merge a JsonObject with a non-JsonObject type.");
                        
                        case JsonArray when toMergeNode.Value is not JsonArray:
                            throw new InvalidOperationException("Cannot merge a JsonArray with a non-JsonArray type.");
                        
                        // recursively merge two JsonObjects
                        case JsonObject originalKeyObject when toMergeNode.Value is JsonObject toMergeKeyObject:
                            jsonBaseObj[toMergeNode.Key] = originalKeyObject.Merge(toMergeKeyObject);
                            break;

                        // recursively merge two JsonArrays
                        case JsonArray originalKeyArray when toMergeNode.Value is JsonArray toMergeKeyArray:
                            jsonBaseObj[toMergeNode.Key] = originalKeyArray.Merge(toMergeKeyArray);
                            break;
                        
                        default:
                            // Indice convention to remove an element
                            if (toMergeNode.Value == null!) {
                                jsonBaseObj.Remove(toMergeNode.Key);
                                break;
                            }
                            
                            jsonBaseObj[toMergeNode.Key] = toMergeNode.Value;
                            break;
                    }
                }

                break;
            }

            case JsonArray originalKeyArray when toMerge is JsonArray toMergeKeyArray: {
                var mergeDictionary = toMergeKeyArray.ToArray();
                toMergeKeyArray.Clear();

                foreach (var toMergeNode in mergeDictionary) {
                    originalKeyArray.Add(toMergeNode);
                }

                break;
            }

            default:
                throw new InvalidOperationException($"The JsonNode type {nameof(original)} is not supported for merging.");
        }

        return original;
    }

    /// <summary>
    /// Constructs and applies a JSON Patch operation to a JsonNode based on a provided JsonArray patch definition.
    /// This should adhere to the JSON Patch RFC specification: https://datatracker.ietf.org/doc/html/rfc6902#appendix-A
    /// </summary>
    /// <param name="original">The original <see cref="JsonNode"/> to apply the patch to.</param>
    /// <param name="jsonPatch">A <see cref="JsonArray"/> containing the patch definition, which must conform to the JSON Patch RFC 6902 standard.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the operation type specified in the patch definition is invalid or unsupported.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the patch application fails or an unrecognized operation type is encountered.</exception>
    public static JsonNode ApplyJsonPatch(this JsonNode original, JsonArray jsonPatch) {

        var merged = original;
        foreach (var operation in jsonPatch) {
            var result = ParseAsJsonPatch(operation).Apply(merged);
            if (!result.IsSuccess) {
                throw new InvalidOperationException("The patch operation could not be applied. Verify that the original JSON and the patch definition are compatible.");
            }

            merged = result.Result;
        }

        return merged;
    }

    /// <summary>Parses the JsonNode To a JsonPatch.</summary>
    private static JsonPatch ParseAsJsonPatch(this JsonNode jsonNode) {
        var jsonObject = (JsonObject)jsonNode;
        if (!jsonObject.TryGetPropertyValue("path", out var pathNode) || !jsonObject.TryGetPropertyValue("op", out var opNode)) {
            throw new ArgumentException(@"The patch definition must include 'path' and 'op' fields.", nameof(jsonObject));
        }

        var op = opNode?.GetValue<string>() ?? throw new ArgumentNullException(nameof(opNode));
        var path = pathNode?.GetValue<string>() ?? throw new ArgumentNullException(nameof(pathNode));

        if (!Enum.TryParse<OperationType>(CapitalizeFirstChar(op), out var operationType)) {
            throw new ArgumentException(@"The patch definition is invalid. Ensure the 'op' field is present and correctly formatted.", nameof(jsonObject));
        }

        var operationArray = operationType switch {
            OperationType.Add => PatchOperation.Add(JsonPointer.Parse(path), ExtractJsonPatchValue(jsonObject)),
            OperationType.Replace => PatchOperation.Replace(JsonPointer.Parse(path), ExtractJsonPatchValue(jsonObject)),
            OperationType.Move => PatchOperation.Move(JsonPointer.Parse(jsonObject["from"]!.GetValue<string>()), JsonPointer.Parse(path)),
            OperationType.Remove => PatchOperation.Remove(JsonPointer.Parse(path)),
            OperationType.Copy => PatchOperation.Copy(JsonPointer.Parse(jsonObject["from"]!.GetValue<string>()), JsonPointer.Parse(path)),
            OperationType.Test => PatchOperation.Test(JsonPointer.Parse(path), ExtractJsonPatchValue(jsonObject)),
            _ => throw new NotSupportedException($"The operation '{op}' is not supported. Valid operations are: 'add', 'replace', 'remove', 'copy', 'move', and 'test'.")
        };

        return new JsonPatch(operationArray);
    }

    private static JsonNode ExtractJsonPatchValue(this JsonNode jsonPatch) {
        var value = jsonPatch["value"] is JsonValue ? jsonPatch["value"].GetValue<object>() : jsonPatch["value"];
        return JsonNode.Parse(utf8Json: JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptionDefaults.GetDefaultSettings()));
    }

    private static string CapitalizeFirstChar(this string input) {
        return input switch {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($@"{nameof(input)} cannot be empty", nameof(input)),
            _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
        };
    }
}