using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Server.Manager.Models;
/// <summary>File upload request</summary>
public class ImageUploadRequest
{
    /// <summary>File data</summary>
    [Required]
    public IFormFile? File { get; set; }
    /// <summary>Zoom level. Defaults to 1.0</summary>
    public double? Scale { get; set; }
    /// <summary>offset X axis. Defaults to 0.</summary>
    /// <remarks>Offsets are essentially panning </remarks>
    public int? TranslateX { get; set; }
    /// <summary>offset Y axis. Defaults to 0</summary>
    public int? TranslateY { get; set; }
    /// <summary>
    /// The side size of the viewport square used to crop the image source. 
    /// This is used as a reference for converting the <see cref="TranslateX"/> and <see cref="TranslateY"/> to the internal crop sqare the will be used as the final size of the picture.
    /// If left empty it is asumed as if it is the same as the size of the internal crop square.
    /// </summary>
    public int? ViewPort { get; set; }

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
            Scale = double.TryParse(form[nameof(Scale)], CultureInfo.InvariantCulture, out var scale) ? scale : null,
            TranslateX = int.TryParse(form[nameof(TranslateX)], CultureInfo.InvariantCulture, out var translateX) ? translateX : null,
            TranslateY = int.TryParse(form[nameof(TranslateY)], CultureInfo.InvariantCulture, out var translateY) ? translateY : null,
            ViewPort = int.TryParse(form[nameof(ViewPort)], CultureInfo.InvariantCulture, out var viewPort) ? viewPort : null
        };
    }
}
