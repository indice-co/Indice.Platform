using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Interfaces;

namespace Indice.Features.Cases.Extensions;

/// <summary>JsonMergeExtensions.</summary>
public static class JsonMergeExtensions
{
    /// <summary>Patches Case Data using a Json Serializable object.</summary>
    public static async Task PatchCaseData<TValue>(this IAdminCaseService adminCaseService, ClaimsPrincipal user, Guid caseId, TValue patch) where TValue : notnull 
        => await adminCaseService.PatchCaseData(user, caseId, patch.ParseAsJsonNode() ?? throw new ArgumentNullException(nameof(patch),@"Patch Operation cannot be null."));

    /// <summary>
    /// Parses a Json Serializable object into a <see cref="JsonNode"/>.
    /// Can handle NewtonSoft objects, strings, numeric/bool strings, json strings.
    /// null is returned for null and "null" to preserve sanity of mind.
    /// </summary>
    /// 
    /// <exception cref="NotSupportedException">
    /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
    /// for <typeparamref name="TValue"/> or its serializable members.
    /// </exception>
    public static JsonNode? ParseAsJsonNode<TValue>(this TValue value) where TValue: notnull {
        return value switch {
            JsonNode jsonNode => jsonNode,
            _ => JsonSerializer.Deserialize<JsonNode>(JsonSerializer.Serialize(value)),
        };
    }
    
    /// <summary>
    /// Recursively merges two JsonNodes by ensuring that the structure of the `original` node is
    /// updated defensively preventing overwrites of incompatible JsonNode types.
    /// 1. If the `toMerge` node contains multiple nested types, each one of them should exist as-is in the `original` node.
    /// 2. If the `toMerge` node contains nested types that the `original` node only partially matches (subset),
    ///   the remaining nested types are added to the corresponding location in the `original` node.
    /// 3. When we encounter a JsonArray we add/replace from THIS POINT ON the nested element at the end of the array:
    /// a. We cannot reliably specify the index of the json array in order to replace the property.
    /// b. Json semantics do not provide any way of uniqueness on array values.
    /// c. We do not want to enforce any keyword to signify an action - add or replace-
    /// or an id in the case of nested objects - which should exist for most business data - on the API for now.
    ///
    /// <remarks>
    /// For handling nested arrays, moving elements, checking if data exists at specified locations use JsonPatch API
    /// </remarks>
    /// 
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if JsonNode types are not the same or if trying to merge JsonValue.
    /// </exception>
    public static JsonNode? Merge(this JsonNode? original, JsonNode? toMerge) {
        if (original == null || toMerge == null) {
            return original;
        }
        
        switch (original) {
            case JsonObject jsonBaseObj when toMerge is JsonObject tempMergeObj: {
                var mergeDictionary = tempMergeObj.ToArray();
                tempMergeObj.Clear();

                foreach (var toMergeNode in mergeDictionary) {
                    switch (jsonBaseObj[toMergeNode.Key]) {
                        case JsonObject when toMergeNode.Value is null:
                            jsonBaseObj.Remove(toMergeNode.Key);
                            break;
                        
                        case JsonArray when toMergeNode.Value is null:
                            jsonBaseObj.Remove(toMergeNode.Key);
                            break;
                        
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
                    if (toMergeNode is null) {
                        originalKeyArray.Remove(toMergeNode);
                        continue;
                    }
                    
                    originalKeyArray.Add(toMergeNode);
                }

                
                break;
            }

            default:
                throw new InvalidOperationException($"The JsonNode type {nameof(original)} is not supported for merging.");
        }

        return original;
    }
}