using System;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models.Kpis;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Types;

namespace Indice.Features.Messages.Core.Services.Abstractions;

/// <summary>Provides methods for retrieving message event data.</summary>
public interface IMessageEventService
{
    /// <summary>Retrieves a list of message events based on the provided options.</summary>
    Task<ResultSet<MessageEvent>> GetListAsync(ListOptions<MessageEventListFilter> options);
    /// <summary>Retrieves a list of message events based on the provided options.</summary>
    Task<ResultSet<MessageEventSeries, MessageEventSeriesSummary>> GetSeriesAsync(MessageEventSeriesFilter filter);
}
