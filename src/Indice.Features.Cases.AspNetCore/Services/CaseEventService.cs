using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases.Services
{
    public class CaseEventService : ICaseEventService
    {
        private readonly IServiceProvider _serviceProvider;

        public CaseEventService(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task Publish<TEvent>(TEvent @event) 
            where TEvent : ICaseEvent {
            var handlers = (IEnumerable<ICaseEventHandler<TEvent>>)_serviceProvider.GetService(typeof(IEnumerable<ICaseEventHandler<TEvent>>));
            foreach (var handler in handlers) {
                await handler.Handle(@event);
            }
        }
    }
}