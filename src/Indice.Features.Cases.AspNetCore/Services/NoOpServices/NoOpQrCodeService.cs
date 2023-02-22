using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases.Services.NoOpServices
{
    internal class NoOpQrCodeService : IQrCodeService
    {
        public byte[] Add(byte[] pdf, Guid? id) {
            throw new NotImplementedException("Implement this interface with your own data sources.");
        }
    }
}
