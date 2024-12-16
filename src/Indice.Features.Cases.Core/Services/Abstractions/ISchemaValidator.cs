using System.Text.Json.Nodes;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>The JSON schema validator service.</summary>
public interface ISchemaValidator
{

    /// <summary>
    /// Check a dynamic json data object against a json schema in text form
    /// </summary>
    /// <param name="schema">The given json schema to validate against</param>
    /// <param name="data">The intance data to be validated. In case the data are not in json form they will be serialized first</param>
    /// <returns>True in case of a valid schema, false otherwize</returns>
    /// <remarks>https://github.com/gregsdennis/json-everything</remarks>
    bool IsValid(string schema, dynamic data);
}

/// <summary>The <see cref="ISchemaValidator"/> service extensions.</summary>
public static class ISchemaValidatorExtensions
{

    /// <summary>
    /// Check a dynamic json data object against a json schema in text form
    /// </summary>
    /// <param name="schemaValidator">The schema validator</param>
    /// <param name="schema">The json schema in text form to validate against</param>
    /// <param name="data">The intance data to be validated. In case the data are not in json form they will be serialized first</param>
    /// <returns>True in case of a valid schema, false otherwize</returns>
    /// <remarks>https://github.com/gregsdennis/json-everything</remarks>
    public static bool IsValid<TData>(this ISchemaValidator schemaValidator, JsonNode schema, TData data) => schemaValidator.IsValid(schema.ToJsonString(), data!);
}
