namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>The JSON schema validator service.</summary>
public interface ISchemaValidator
{
    /// <summary>Validate json data is valid against a json schema.</summary>
    /// <remarks>https://github.com/gregsdennis/json-everything</remarks>
    /// <param name="schema">The json schema.</param>
    /// <param name="data">The json data.</param>
    bool IsValid(string schema, dynamic data);
}
