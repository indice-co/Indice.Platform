namespace Indice.Services;

/// <summary>Represents a webhook handler that processes a list of <see cref="WebhookEvent"/>.</summary>
public interface IWebhookHandler
{
    /// <summary>Parses the incoming webhook request and converts it into a list of <see cref="WebhookEvent"/>.</summary>
    Task HandleAsync(List<WebhookEvent> events, CancellationToken cancellationToken);
}

/// <summary>A no-operation implementation of <see cref="IWebhookHandler"/> that does nothing when handling webhook events.</summary>
public sealed class NoOpWebhookHandler : IWebhookHandler
{
    /// <inheritdoc/>
    public async Task HandleAsync(List<WebhookEvent> events, CancellationToken cancellationToken) => await Task.CompletedTask;
}