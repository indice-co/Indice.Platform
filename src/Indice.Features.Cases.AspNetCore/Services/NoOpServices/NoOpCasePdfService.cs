using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;

namespace Indice.Features.Cases.Services.NoOpServices
{
    internal class NoOpCasePdfService : ICasePdfService
    {
        public Task<byte[]> HtmlToPdfAsync(string htmlTemplate, PdfOptions pdfOptions, Case @case) =>
            throw new NotImplementedException("Implement this interface with your own data sources.");
    }
}
