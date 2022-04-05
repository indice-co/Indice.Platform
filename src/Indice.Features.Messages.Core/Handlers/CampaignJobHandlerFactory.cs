using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.Core.Handlers
{
    /// <summary>
    /// A factory class that creates instance of <see cref="ICampaignJobHandler{TEvent}"/> implementations based on the type of event.
    /// </summary>
    public class CampaignJobHandlerFactory
    {
        /// <summary>
        /// Creates a new instance of <see cref="CampaignJobHandlerFactory"/>.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public CampaignJobHandlerFactory(IServiceProvider serviceProvider) {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        private IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Creates the appropriate instance based on handled event.
        /// </summary>
        /// <typeparam name="TEvent">The type of event.</typeparam>
        public ICampaignJobHandler<TEvent> Create<TEvent>() where TEvent : class {
            var handler = ServiceProvider.GetService<ICampaignJobHandler<TEvent>>();
            return handler;
        }
    }
}
