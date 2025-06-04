using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;

namespace Indice.Features.Cases.Core.Services.NoOpServices;

internal class NoOpCasePdfService : ICasePdfService
{
    public Task<byte[]> HtmlToPdfAsync(string htmlTemplate, PdfOptions pdfOptions, Case @case) =>
        throw new NotImplementedException("Implement this interface with your own data sources.");
}
