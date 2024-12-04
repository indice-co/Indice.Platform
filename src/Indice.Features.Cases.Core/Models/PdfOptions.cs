using System.Text.Json;
using System.Text.Json.Nodes;

namespace Indice.Features.Cases.Core.Models;

/// <summary>PdfOptions</summary>
public class PdfOptions
{
    /// <summary>PdfOptions</summary>
    /// <param name="caseTypeConfig">The case Type Config.</param>
    public PdfOptions(string? caseTypeConfig) {
        if (!string.IsNullOrEmpty(caseTypeConfig)) {
            var node = JsonSerializer.Deserialize<JsonNode>(caseTypeConfig)!;
            var pdfOptions = node[nameof(PdfOptions)];

            if (pdfOptions is not null) {
                IsPortrait = pdfOptions[nameof(IsPortrait)]?.GetValue<bool?>();
                DigitallySigned = pdfOptions[nameof(DigitallySigned)]?.GetValue<bool?>();
                RequiresQrCode = pdfOptions[nameof(RequiresQrCode)]?.GetValue<bool?>();
            }
        }
    }
    /// <summary>IsPortrait</summary>
    public bool? IsPortrait { get; set; } = true;
    /// <summary>DigitallySigned</summary>
    public bool? DigitallySigned { get; set; } = false;
    /// <summary>RequiresQrCode</summary>
    public bool? RequiresQrCode { get; set; } = false;
}
