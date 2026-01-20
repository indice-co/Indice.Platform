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
    /// <returns>A task representing the asynchronous validation operation.</returns>
    /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">Thrown when the group name is invalid.</exception>
    Task ValidateAsync(string groupName);
}
