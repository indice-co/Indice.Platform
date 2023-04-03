using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Server.Manager.Models;
/// <summary>File upload request</summary>
public class FileUploadRequest
{
    [Required]
    /// <summary>File data</summary>
    public IFormFile File { get; set; }

    /// <summary>
    /// Bind method
    /// </summary>
    /// <param name="context"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
    public static async ValueTask<FileUploadRequest> BindAsync(HttpContext context,
                                                   ParameterInfo parameter) {
        var form = await context.Request.ReadFormAsync();
        var file = form.Files[nameof(File)];
        return new FileUploadRequest {
            File = file
        };
    }
}
