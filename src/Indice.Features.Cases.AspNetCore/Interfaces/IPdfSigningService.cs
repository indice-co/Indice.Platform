namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// The service for signing PDF documents.
    /// </summary>
    public interface IPdfSigningService
    {
        /// <summary>
        /// Digitally sign a pdf document.
        /// </summary>
        /// <param name="pdf">The pdf document to be signed.</param>
        /// <returns></returns>
        byte[] Sign(byte[] pdf);
    }
}
