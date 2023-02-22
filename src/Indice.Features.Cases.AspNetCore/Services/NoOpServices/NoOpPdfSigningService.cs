using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases.Services.NoOpServices
{
    internal class NoOpPdfSigningService : IPdfSigningService
    {
        public byte[] Sign(byte[] pdf) {
            throw new NotImplementedException("Implement this interface with your own data sources.");
        }
    }
}
