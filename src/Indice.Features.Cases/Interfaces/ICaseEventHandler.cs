using System.Threading.Tasks;

namespace Indice.Features.Cases.Interfaces
{
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