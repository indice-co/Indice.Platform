using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Indice.Services
{
    /// <inheritdoc/>
    public class EventDispatcherInMemory : IEventDispatcher
    {
        private readonly Queue<object> _queue = new Queue<object>();

        /// <inheritdoc/>
        public Task RaiseEventAsync<TEvent>(TEvent payload, ClaimsPrincipal actingPrincipal = null, TimeSpan? initialVisibilityDelay = null, bool wrap = true, bool base64 = false) where TEvent : class, new() {
            _queue.Enqueue(payload);
#if NET452
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }
    }
}
