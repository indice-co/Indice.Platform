using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Indice.Events;
using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs;

/// <inheritdoc/>
public class DefaultActivityLogFromEventConverter : IActivityLogFromEventConverter
{
    /// <inheritdoc/>
    public ActivityLogEntry? Convert(IPlatformEvent @event) => new() {
        Category = @event.GetType().Name,
        Description = @event.GetType().Name.Humanize()
    };
}
