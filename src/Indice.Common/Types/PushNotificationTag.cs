namespace Indice.Types
{
    /// <summary></summary>
    public readonly struct PushNotificationTag
    {
        /// <summary>Creates a new instance of <see cref="PushNotificationTag"/>, providing the <paramref name="value"/> and <paramref name="kind"/> parameters..</summary>
        /// <param name="value">The value of the tag.</param>
        /// <param name="kind">Indicates whether the tag refers to the user or the device.</param>
        public PushNotificationTag(string value, PushNotificationTagKind kind) {
            Value = value ?? throw new System.ArgumentNullException(nameof(value));
            Kind = kind;
        }

        /// <summary>Creates a new instance of <see cref="PushNotificationTag"/>, providing the <paramref name="value"/> and parameter and use <see cref="PushNotificationTagKind.Unspecified"/> as default <see cref="Kind"/>.</summary>
        /// <param name="value">The value of the tag.</param>
        public PushNotificationTag(string value) : this(value, PushNotificationTagKind.Unspecified) { }

        /// <summary>The value of the tag.</summary>
        public string Value { get; }
        /// <summary>Indicates whether the tag refers to the user or the device.</summary>
        public PushNotificationTagKind Kind { get; }

        /// <summary>Implicit conversion of <see cref="PushNotificationTag"/> to string.</summary>
        /// <param name="tag">The <see cref="PushNotificationTag"/> instance.</param>
        public static implicit operator string(PushNotificationTag tag) => tag.ToString();

        /// <inheritdoc />
        public override string ToString() => Kind == PushNotificationTagKind.Unspecified
            ? Value
            : $"{Kind.ToString().ToLowerInvariant()}:{Value}";
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
}
