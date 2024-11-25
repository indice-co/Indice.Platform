#nullable enable
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Messages.AspNetCore.Endpoints;
/// <summary>
/// Represents an uploaded file request.
/// </summary>
public class UploadFileRequest
{
    /// <summary>
    /// A file attached
    /// </summary>
    [Required]
    public IFormFile? File { get; set; }


    /// <summary>
    /// Bind method
    /// </summary>
    /// <param name="context"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
    public static async ValueTask<UploadFileRequest> BindAsync(HttpContext context, ParameterInfo parameter) {
        var form = await context.Request.ReadFormAsync();
        var file = form.Files[nameof(File)];
        return new UploadFileRequest {
            File = file,
        };
    }

}
#nullable disable