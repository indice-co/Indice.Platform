#if NET7_0_OR_GREATER
using Indice.Extensions;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Http;


/// <summary>Endpoint allow file limit</summary>
public static class LimitUploadFilter
{

    /// <summary>
    /// Sets the allowed 
    /// <list type="bullet">
    ///   <item>maximum <strong>file size</strong> for the request body </item>
    ///   <item>accepted <strong>file extensions</strong> for the request body</item>
    /// </list>
    /// </summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">the builder</param>
    /// <param name="sizeLimit">The maximum allowed file size in bytes.</param>
    /// <param name="fileExtensions">Allowed file extensions as a comma or space separated string.</param>
    /// <returns>The builder</returns>
    public static TBuilder LimitUpload<TBuilder>(this TBuilder builder, long sizeLimit, string fileExtensions = null)
        where TBuilder : IEndpointConventionBuilder {
        builder.Add(eb => {
            var allowedExtensions = fileExtensions?.Split(' ', ',', ';')
                                            .Where(x => null != x)
                                            .Select(x => '.' + x.Trim().TrimStart('.'))
                                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
            // We can respond with problem details if there's a validation error
            eb.Metadata.Add(new ProducesResponseTypeMetadata(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json"));

            eb.FilterFactories.Add((context, next) => {

                return new EndpointFilterDelegate(async (efic) => {
                    var httpContext = efic.HttpContext;
                    var errors = ValidationErrors.Create();
                    foreach (var file in httpContext.Request.Form.Files) {
                        var ext = Path.GetExtension(file.FileName);
                        if (allowedExtensions is not null && !allowedExtensions.Contains(ext)) {
                            errors.AddError(file.FileName, $"File with extension {Path.GetExtension(file.FileName)} is not permitted. Allowed file extensions are {string.Join(", ", allowedExtensions)}");
                        }
                        if (file.Length > sizeLimit) {
                            errors.AddError(file.FileName, $"File size cannot exceed {sizeLimit.ToFileSize()}.");
                        }
                    }
                    if (errors.Count > 0) { 
                        return Results.ValidationProblem(errors, detail: "File not allowed");
                    }
                    return await next(efic);
                });
            });
        });
        return builder;
    }
}
#endif