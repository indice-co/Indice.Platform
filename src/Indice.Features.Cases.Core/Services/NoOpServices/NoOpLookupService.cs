using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;

namespace Indice.Features.Cases.Core.Services.NoOpServices;

internal class NoOpLookupService : ILookupService
{
    public string Name => nameof(NoOpLookupService);

    public Task<ResultSet<LookupItem>> Get(ListOptions<LookupFilter>? options = null) =>
        throw new NotImplementedException("Implement this interface with your own data sources.");
}