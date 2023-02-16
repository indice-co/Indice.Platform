namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// The pdf service for generating PDF from case data.
    /// </summary>
    public interface ICasePdfService
    {
        /// <summary>
        /// Render a html template to PDF.
        /// </summary>
        /// <param name="htmlTemplate">The html template.</param>
        /// <param name="isPortrait">The pdf print option for defining portrait.</param>
        /// <param name="digitallySigned">Determines whether the pdf document will be digitally signed.</param>
        /// <param name="requiresQrCode">Determines whether the pdf document will have a QR code.</param>
        /// <param name="caseId">The Id of the case.</param>
        /// <returns></returns>
        Task<byte[]> HtmlToPdfAsync(string htmlTemplate, bool isPortrait = true, bool digitallySigned = false, bool requiresQrCode = false, Guid? caseId = null);
    }
}
