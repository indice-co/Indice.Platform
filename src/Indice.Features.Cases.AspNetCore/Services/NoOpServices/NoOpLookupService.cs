using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Services.NoOpServices;

internal class NoOpLookupService : ILookupService
{
    public string Name => nameof(NoOpLookupService);

    public Task<ResultSet<LookupItem>> Get(ListOptions<List<FilterClause>> options = null) =>
        throw new NotImplementedException("Implement this interface with your own data sources.");
}