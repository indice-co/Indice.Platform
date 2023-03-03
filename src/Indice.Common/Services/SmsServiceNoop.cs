namespace Indice.Services;

/// <summary>A default implementation for <see cref="ISmsService"/> that does nothing.</summary>
public class SmsServiceNoop : ISmsService
{
    /// <inheritdoc />
    public Task SendAsync(string destination, string subject, string body, SmsSender sender = null) => Task.CompletedTask;

    /// <inheritdoc />
    public bool Supports(string deliveryChannel) => true;
}