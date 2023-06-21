using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Abstractions;

internal interface IEventStore
{
    Task<int> CreateAsync(TransactionEvent @event);
}
