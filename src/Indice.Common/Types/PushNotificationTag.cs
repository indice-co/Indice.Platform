using System;
using System.Text.RegularExpressions;

namespace Indice.Types;

/// <summary></summary>
public readonly struct PushNotificationTag : IEquatable<PushNotificationTag>
{
    /// <summary>Creates a new instance of <see cref="PushNotificationTag"/>, providing the <paramref name="value"/> and <paramref name="kind"/> parameters..</summary>
    /// <param name="value">The value of the tag.</param>
    /// <param name="kind">Indicates whether the tag refers to the user or the device.</param>
    public PushNotificationTag(string value, PushNotificationTagKind kind) {
        Value = value ?? string.Empty;
        Kind = kind;
    }

    /// <summary>Creates a new instance of <see cref="PushNotificationTag"/>, providing the <paramref name="value"/> and parameter and use <see cref="PushNotificationTagKind.Unspecified"/> as default <see cref="Kind"/>.</summary>
    /// <param name="value">The value of the tag.</param>
    public PushNotificationTag(string value) : this(value, PushNotificationTagKind.Unspecified) { }

    /// <summary>The value of the tag.</summary>
    public string Value { get; }
    /// <summary>Indicates whether the tag refers to the user or the device.</summary>
    public PushNotificationTagKind Kind { get; }

    /// <summary>Parses a string into its <see cref="PushNotificationTag"/> representation.</summary>
    /// <param name="tag">Input tag.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static PushNotificationTag Parse(string tag) {
        if (tag is null) {
            throw new ArgumentNullException(nameof(tag));
        }
        var tagSegments = tag.Split(':');
        if (tagSegments.Length >= 2) {
            var kind = Enum.Parse<PushNotificationTagKind>(tagSegments[0], ignoreCase: true);
            var value = new Regex($"{kind.ToString().ToLowerInvariant()}:").Replace(tagSegments[1], string.Empty, 1);
            return new PushNotificationTag(value, kind);
        }
        return new PushNotificationTag(tagSegments[0]);
    }

    /// <summary>Parses a string into its <see cref="PushNotificationTag"/> representation.</summary>
    /// <param name="tag">Input tag.</param>
    /// <param name="pushNotificationTag"></param>
    public static bool TryParse(string tag, out PushNotificationTag pushNotificationTag) {
        pushNotificationTag = new PushNotificationTag();
        try {
            pushNotificationTag = Parse(tag);
            return true;
        } catch (Exception) {
            return false;
        }
    }

    /// <inheritdoc />
    public bool Equals(PushNotificationTag other) => Kind == other.Kind && Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object other) => other is PushNotificationTag tag && Equals(tag);

    /// <inheritdoc />
    public override string ToString() => Kind == PushNotificationTagKind.Unspecified ? Value : $"{Kind.ToString().ToLowerInvariant()}:{Value}";

    /// <inheritdoc />
    public override int GetHashCode() => (Kind, Value).GetHashCode();

    /// <summary>Implicit conversion of <see cref="PushNotificationTag"/> to string.</summary>
    /// <param name="tag">The <see cref="PushNotificationTag"/> instance.</param>
    public static implicit operator string(PushNotificationTag tag) => tag.ToString();

    /// <inheritdoc />
    public static bool operator ==(PushNotificationTag leftTag, PushNotificationTag rightTag) => leftTag.Equals(rightTag);

    /// <inheritdoc />
    public static bool operator !=(PushNotificationTag leftTag, PushNotificationTag rightTag) => !(leftTag == rightTag);
}

/// <summary></summary>
public enum PushNotificationTagKind
{
    /// <summary>Unspecified</summary>
    Unspecified,
    /// <summary>User</summary>
    User,
    /// <summary>Device</summary>
    Device
}
