namespace Indice.Features.Messages.Core.Models;

/// <summary>The delivery channel of a campaign.</summary>
[Flags]
public enum MessageChannelKind : byte
{
    /// <summary>No delivery.</summary>
    None = 0,
    /// <summary>Campaign is displayed on user inbox.</summary>
    Inbox = 1,
    /// <summary>Campaign is sent as push notification.</summary>
    PushNotification = 2,
    /// <summary>Campaign is sent as email.</summary>
    Email = 4,
    /// <summary>Campaign is sent as SMS.</summary>
    SMS = 8
}
/// <summary>Extension methods for <see cref="MessageChannelKind"/>.</summary>
public static class MessageChannelKindExtensions
{
    /// <summary>
    /// Checks if the <see cref="MessageChannelKind"/> is set to a value other than <see cref="MessageChannelKind.None"/>.
    /// </summary>
    /// <param name="enumValue"></param>
    /// <returns></returns>
    public static bool IsSet(this MessageChannelKind enumValue) => enumValue != MessageChannelKind.None;

    /// <summary>
    /// Checks if the <see cref="MessageChannelKind"/> has a specific flag set. 
    /// </summary>
    /// <param name="enumValue"></param>
    /// <returns></returns>
    public static List<MessageChannelKind> ToList(this MessageChannelKind enumValue) {
        var result = new List<MessageChannelKind>();
        if (enumValue.IsSet()) {
            foreach (MessageChannelKind value in Enum.GetValues(typeof(MessageChannelKind))) {
                if (MessageChannelKind.None != value && enumValue.HasFlag(value)) {
                    result.Add(value);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Converts a collection of <see cref="MessageChannelKind"/> to a single flags value.
    /// </summary>
    /// <param name="enumValues"></param>
    /// <returns></returns>
    public static MessageChannelKind ToFlags(this IEnumerable<MessageChannelKind> enumValues) => (MessageChannelKind)enumValues.Select(x => (int)x).Sum();
}