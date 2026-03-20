using System.Net.Mime;
using Indice.AspNetCore.Configuration;
using Indice.Extensions;
using Indice.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http;

/// <summary>Endpoint allow file size limit and extension.</summary>
public static class LimitUploadFilter
{
    /// <summary>
    /// Sets the allowed:
    /// <list type="bullet">
    ///   <item>maximum <strong>file size</strong> for the request body </item>
    ///   <item>accepted <strong>file extensions</strong> for the request body</item>
    /// </list>
    /// </summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder">Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.</param>
    /// <param name="sizeLimit">The maximum allowed file size in bytes.</param>
    /// <param name="fileExtensions">Allowed file extensions as a comma or space separated string.</param>
    /// <param name="enableMagicByteValidation">Overrides magic bytes validation option.</param>
    /// <returns>The builder.</returns>
    public static TBuilder LimitUpload<TBuilder>(this TBuilder builder, long sizeLimit, string? fileExtensions = null, bool? enableMagicByteValidation = null) 
        where TBuilder : IEndpointConventionBuilder {
        builder.Add(endpointBuilder => {
            var allowedExtensions = fileExtensions?
                .Split(' ', ',', ';')
                .Where(x => null != x)
                .Select(x => '.' + x.Trim().TrimStart('.'))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            endpointBuilder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest, typeof(HttpValidationProblemDetails), [ MediaTypeNames.Application.ProblemJson ]));
            endpointBuilder.FilterFactories.Add((context, next) => {
                return new EndpointFilterDelegate(async invocationContext => {
                    var httpContext = invocationContext.HttpContext;
                    var options = httpContext.RequestServices.GetRequiredService<IOptions<LimitUploadOptions>>().Value;
                    var magicBytesValidator = httpContext.RequestServices.GetService<IMagicBytesValidator>();
                    var errors = ValidationErrors.Create();
                    foreach (var file in httpContext.Request.Form.Files) {
                        var extension = Path.GetExtension(file.FileName);
                        if (allowedExtensions is not null && !allowedExtensions.Contains(extension)) {
                            errors.AddError(file.FileName, $"File with extension {Path.GetExtension(file.FileName)} is not permitted. Allowed file extensions are {string.Join(", ", allowedExtensions)}");
                        }
                        if (file.Length > sizeLimit) {
                            errors.AddError(file.FileName, $"File size cannot exceed {sizeLimit.ToFileSize()}.");
                        }

                        var validateMagicBytes = enableMagicByteValidation ?? options.EnableMagicByteValidation;
                        if (validateMagicBytes && magicBytesValidator is not null) {
                            await using var memoryStream = new MemoryStream((int)file.Length);
                            await file.CopyToAsync(memoryStream);
                            ReadOnlySpan<byte> fileSpan = memoryStream.GetBuffer().AsSpan(0, (int)memoryStream.Length);
                            var isValid = magicBytesValidator.IsValid(fileSpan, extension);
                            if (!isValid) {
                                errors.AddError(file.FileName, $"File content does not match the expected format for extension {extension}.");
                            }
                        }
                    }
                    if (errors.Count > 0) {
                        return Results.ValidationProblem(errors, detail: "File not allowed");
                    }
                    return await next(invocationContext);
                });
            });
        });
        return builder;
    }
}