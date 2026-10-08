#if NET10_0_OR_GREATER
namespace Indice.AspNetCore.OpenApi.Attributes;

/// <summary>
/// Add this attribute to a property to ignore it from the OpenAPI document. This is useful for properties that are used for internal purposes 
/// and should not be exposed in the API documentation.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class OpenApiIgnoreAttribute : Attribute { }

#endif