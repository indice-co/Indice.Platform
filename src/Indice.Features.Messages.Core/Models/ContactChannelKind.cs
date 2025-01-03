namespace Indice.Features.Messages.Core.Models;

/// <summary>The delivery channel preference of a contact.</summary>
[Flags]
public enum ContactChannelKind : byte
{
    /// <summary>Use any channel available.</summary>
    Any = 0,
    /// <summary>Use email channel.</summary>
    Email = 1,
    /// <summary>Use SMS channel.</summary>
    SMS = 2,
    /// <summary>Use push notification channel.</summary>
    PushNotification = 4
}