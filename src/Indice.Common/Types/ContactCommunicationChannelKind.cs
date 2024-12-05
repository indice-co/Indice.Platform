namespace Indice.Features.Messages.Core.Models;

/// <summary>The delivery channel of a campaign.</summary>
[Flags]
public enum ContactCommunicationChannelKind : byte
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