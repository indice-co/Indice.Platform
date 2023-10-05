using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Media.AspNetCore.Models.Requests;

/// <summary>The request model used to upload a file.</summary>
public class UploadFileRequest
{
    /// <summary>The folder in which the file will be uploaded.</summary>
    public Guid? FolderId { get; set; }
    /// <summary>The file to be uploaded.</summary>
    [Required]
    public IFormFile? File { get; set; }

    /// <summary> Binds the File from Form to Request property. </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="parameter">The Parameter metadata</param>
    /// <returns></returns>
    public static async ValueTask<UploadFileRequest> BindAsync(HttpContext context, ParameterInfo parameter) {
        var form = await context.Request.ReadFormAsync();
        var files = form.Files;
        if (files is null || !files.Any()) {
            throw new ArgumentNullException(nameof(files));
        }
        Guid? folderId = null;
        if (form.ContainsKey(nameof(FolderId)) && Guid.TryParse(form[nameof(FolderId)], out var folderIdValue)) {
            folderId = folderIdValue;
        }
        return new UploadFileRequest {
            File = files[0],
            FolderId = folderId
        };
    }
}
