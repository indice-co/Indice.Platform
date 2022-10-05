using System.Threading.Tasks;

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
        /// <returns></returns>
        Task<byte[]> HtmlToPdfAsync(string htmlTemplate, bool isPortrait = true, bool digitallySigned = false);
    }
}
