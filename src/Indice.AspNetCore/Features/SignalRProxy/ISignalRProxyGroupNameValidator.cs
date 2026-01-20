namespace Indice.AspNetCore.Features.SignalRProxy;

/// <summary>
/// Interface for validating SignalR group names.
/// </summary>
public interface ISignalRProxyGroupNameValidator
{
    /// <summary>
    /// Validates the specified group name.
    /// </summary>
    /// <param name="groupName">The group name to validate.</param>
    /// <returns>A task that represents the asynchronous validation operation. The task result contains true if the group name is valid; otherwise, false.</returns>
    Task<bool> ValidateAsync(string groupName);
}
