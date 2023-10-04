using Indice.Features.Messages.Core.Services.Abstractions;

namespace Indice.Features.Messages.Core;

/// <summary> The base options for worker configuration. </summary>
public class MessageWorkerOptions
{
    /// <summary> The period in days until a contact is updated with latest values from <see cref="IContactResolver"/></summary>
    public int ContactRetainPeriodInDays { get; set; } = 5;
}
