using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Events;
using Indice.Services;
using Microsoft.Extensions.Logging;

namespace Indice.Identity.Services
{
    public class DeviceDeletedEventHandler : IPlatformEventHandler<DeviceDeletedEvent>
    {
        private readonly ILogger<DeviceDeletedEventHandler> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="DeviceDeletedEventHandler"/>.
        /// </summary>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public DeviceDeletedEventHandler(ILogger<DeviceDeletedEventHandler> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handle device deleted event.
        /// </summary>
        /// <param name="event">Device deleted event info.</param>
        public Task Handle(DeviceDeletedEvent @event) {
            _logger.LogDebug($"Device deleted: {@event}");
            return Task.CompletedTask;
        }
    }
}
