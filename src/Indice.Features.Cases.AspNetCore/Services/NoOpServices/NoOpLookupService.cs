using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Services.NoOpServices
{
    internal class NoOpLookupService : ILookupService
    {
        public Task<ResultSet<LookupItem>> Get(string lookupName, string searchValues = null) =>
            throw new NotImplementedException("Implement this interface with your own data sources.");
    }
}
