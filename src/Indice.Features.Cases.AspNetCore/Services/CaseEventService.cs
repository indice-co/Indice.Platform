using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases.Services
{
    /// <inheritdoc />
    public class CaseEventService : ICaseEventService
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructs a new <see cref="CaseEventService"/>.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public CaseEventService(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc />
        public async Task Publish<TEvent>(TEvent @event) 
            where TEvent : ICaseEvent {
            var handlers = (IEnumerable<ICaseEventHandler<TEvent>>)_serviceProvider.GetService(typeof(IEnumerable<ICaseEventHandler<TEvent>>));
            foreach (var handler in handlers) {
                await handler.Handle(@event);
            }
        }
    }
}