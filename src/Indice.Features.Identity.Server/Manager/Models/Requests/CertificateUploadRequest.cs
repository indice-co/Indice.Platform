using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Certificate upload request with optional password.</summary>
public class CertificateUploadRequest
{
    /// <summary>File data</summary>
    [Required]
    public IFormFile? File { get; set; }

    /// <summary>
    /// Optional password in case this is a application/x-pkcs12
    /// </summary>
    public string? Password { get; set; }


    /// <summary>
    /// Bind method
    /// </summary>
    /// <param name="context"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
    public static async ValueTask<CertificateUploadRequest> BindAsync(HttpContext context, ParameterInfo parameter) {
        var form = await context.Request.ReadFormAsync();
        var file = form.Files[nameof(File)];
        var password = form[nameof(Password)];
        return new CertificateUploadRequest {
            File = file,
            Password = password,
        };
    }
}
