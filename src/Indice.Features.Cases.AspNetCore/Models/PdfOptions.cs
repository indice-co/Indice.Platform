using System.Text.Json;

namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// PdfOptions
    /// </summary>
    public class PdfOptions
    {
        /// <summary>
        /// PdfOptions
        /// </summary>
        /// <param name="caseTypeConfig">The case Type Config.</param>
        public PdfOptions(string caseTypeConfig) {
            if (!string.IsNullOrEmpty(caseTypeConfig)) {
                var caseTypeConfigJson = JsonSerializer.Deserialize<JsonDocument>(caseTypeConfig);

                if (caseTypeConfigJson.RootElement.TryGetProperty(nameof(PdfOptions), out var pdfOptions)) {
                    if (pdfOptions.TryGetProperty(nameof(IsPortrait), out var isPortrait)) {
                        IsPortrait = isPortrait.GetBoolean();
                    }
                    if (pdfOptions.TryGetProperty(nameof(DigitallySigned), out var digitallySigned)) {
                        DigitallySigned = digitallySigned.GetBoolean();
                    }
                    if (pdfOptions.TryGetProperty(nameof(RequiresQrCode), out var requiresQrCode)) {
                        RequiresQrCode = requiresQrCode.GetBoolean();
                    }
                }
            }
        }
        /// <summary>
        /// IsPortrait
        /// </summary>
        public bool? IsPortrait { get; set; } = true;
        /// <summary>
        /// DigitallySigned
        /// </summary>
        public bool? DigitallySigned { get; set; } = false;
        /// <summary>
        /// RequiresQrCode
        /// </summary>
        public bool? RequiresQrCode { get; set; } = false;
    }
}
