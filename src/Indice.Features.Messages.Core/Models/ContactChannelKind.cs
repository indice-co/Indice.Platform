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

/// <summary>Extension methods for <see cref="ContactChannelKind"/>.</summary>
public static class ContactChannelKindExtensions
{
    /// <summary>
    /// Checks if the <see cref="ContactChannelKind"/> is set to a value other than <see cref="ContactChannelKind.None"/>.
    /// </summary>
    /// <param name="enumValue"></param>
    /// <returns></returns>
    public static bool IsSet(this ContactChannelKind enumValue) => enumValue != ContactChannelKind.Any;

    /// <summary>
    /// Checks if the <see cref="ContactChannelKind"/> has a specific flag set. 
    /// </summary>
    /// <param name="enumValue"></param>
    /// <returns></returns>
    public static List<ContactChannelKind> ToList(this ContactChannelKind enumValue) {
        var result = new List<ContactChannelKind>();
        if (enumValue.IsSet()) {
            foreach (ContactChannelKind value in Enum.GetValues(typeof(ContactChannelKind))) {
                if (ContactChannelKind.Any != value && enumValue.HasFlag(value)) {
                    result.Add(value);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Converts a collection of <see cref="ContactChannelKind"/> to a single flags value.
    /// </summary>
    /// <param name="enumValues"></param>
    /// <returns></returns>
    public static ContactChannelKind ToFlags(this IEnumerable<ContactChannelKind> enumValues) => (ContactChannelKind)enumValues.Select(x => (int)x).Sum();
}