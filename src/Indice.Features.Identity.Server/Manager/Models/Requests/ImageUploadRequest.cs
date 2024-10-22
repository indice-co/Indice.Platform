using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Server.Manager.Models;
/// <summary>File upload request</summary>
public class ImageUploadRequest
{
    /// <summary>File data</summary>
    [Required]
    public IFormFile? File { get; set; }
    /// <summary>Zoom level</summary>
    public double? Zoom { get; set; }
    /// <summary>Center offset X axis</summary>
    public int? OffsetX { get; set; }
    /// <summary>Center offset Y axis</summary>
    public int? OffsetY { get; set; }

    /// <summary>
    /// Bind method
    /// </summary>
    /// <param name="context"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
    public static async ValueTask<ImageUploadRequest> BindAsync(HttpContext context,
                                                   ParameterInfo parameter) {
        var form = await context.Request.ReadFormAsync();
        var file = form.Files[nameof(File)];
        return new ImageUploadRequest {
            File = file,
            Zoom = double.TryParse(form[nameof(Zoom)], out var zoom) ? zoom : null,
            OffsetX = int.TryParse(form[nameof(OffsetX)], out var offsetX) ? offsetX : null,
            OffsetY = int.TryParse(form[nameof(OffsetY)], out var offsetY) ? offsetY : null,
        };
    }
}
