namespace Indice.AspNetCore.Features.SignalRProxy;

/// <summary>
/// A no-op implementation of <see cref="ISignalRProxyGroupNameValidator"/> that always returns true.
/// </summary>
/// <remarks>This is the default implementation that performs no validation.</remarks>
public class NoOpGroupNameValidator : ISignalRProxyGroupNameValidator
{
    /// <inheritdoc />
    public Task<bool> ValidateAsync(string groupName) => Task.FromResult(true);
}
