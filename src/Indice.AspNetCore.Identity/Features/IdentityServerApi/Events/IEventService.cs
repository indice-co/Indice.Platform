using System.Threading.Tasks;

namespace Indice.AspNetCore.Identity.Api.Events
{
    /// <summary>
    /// Models the event mechanism used to raise events inside the IdentityServer API.
    /// </summary>
    internal interface IEventService
    {
        Task Raise<TEvent>(TEvent @event) where TEvent : IIdentityServerApiEvent;
    }
}
