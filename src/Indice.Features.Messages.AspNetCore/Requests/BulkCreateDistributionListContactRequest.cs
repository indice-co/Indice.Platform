using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Messages.AspNetCore.Requests;

/// <summary>
/// The request model for the bulk contacts import command.
/// </summary>
public class BulkCreateDistributionListContactsRequest {
    /// <summary>
    /// Represents the actual file of the request.
    /// </summary>
    [Required]
    public IFormFile? File { get; init; }

    /// <summary>
    /// The binding and validation process for the attached file of the request.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static async ValueTask<BulkCreateDistributionListContactsRequest?> BindAsync(HttpContext context) {
        var form = await context.Request.ReadFormAsync();
        var file = form.Files[nameof(File)];
        if (file is null || file.Length == 0) {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("File is required.");
            return null;
        }
        if (!file.ContentType.Equals(MediaTypeNames.Text.Csv, StringComparison.OrdinalIgnoreCase)) {
            context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
            await context.Response.WriteAsync("Only 'text/csv' content type is supported.");
            return null;
        }
        return new BulkCreateDistributionListContactsRequest {
            File = file
        };
    }
}
