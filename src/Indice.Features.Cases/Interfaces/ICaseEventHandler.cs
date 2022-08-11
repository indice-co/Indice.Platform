using System.Threading.Tasks;

namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// The Case Event handler for <see cref="ICaseEvent"/>.
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public interface ICaseEventHandler<in TEvent> 
        where TEvent :ICaseEvent
    {
        /// <summary>
        /// The method used to handle the event creation.
        /// </summary>
        /// <param name="event">The type of the event raised.</param>
        /// <returns>The <see cref="Task"/> that was successfully completed.</returns>
        Task Handle(TEvent @event); // todo cancellation token support
    }
}