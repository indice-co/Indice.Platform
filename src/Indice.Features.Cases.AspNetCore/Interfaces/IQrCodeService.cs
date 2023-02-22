namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// The service for adding QR Code in PDF documents.
    /// </summary>
    public interface IQrCodeService
    {
        /// <summary>
        /// Add QR code to a pdf document.
        /// </summary>
        /// <param name="pdf">The pdf document.</param>
        /// <param name="id">The QR code's URL id.</param>
        /// <returns></returns>
        byte[] Add(byte[] pdf, Guid? id);
    }
}
