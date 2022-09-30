namespace Indice.Types
{
    /// <summary></summary>
    public readonly struct PushNotificationTag
    {
        /// <summary>Creates a new instance of <see cref="PushNotificationTag"/>, providing the <paramref name="value"/> and <paramref name="target"/> parameters..</summary>
        /// <param name="value">The value of the tag.</param>
        /// <param name="target">Indicates whether the tag refers to the user or the device.</param>
        public PushNotificationTag(string value, PushNotificationTagTarget target) {
            Value = value;
            Target = target;
        }

        /// <summary>Creates a new instance of <see cref="PushNotificationTag"/>, providing the <paramref name="value"/> and parameter and use <see cref="PushNotificationTagTarget.User"/> as default <see cref="Target"/>.</summary>
        /// <param name="value">The value of the tag.</param>
        public PushNotificationTag(string value) : this(value, PushNotificationTagTarget.User) { }

        /// <summary>The value of the tag.</summary>
        public string Value { get; }
        /// <summary>Indicates whether the tag refers to the user or the device.</summary>
        public PushNotificationTagTarget Target { get; }

        /// <summary>Implicit conversion of <see cref="PushNotificationTag"/> to string.</summary>
        /// <param name="tag">The <see cref="PushNotificationTag"/> instance.</param>
        public static implicit operator string(PushNotificationTag tag) => tag.Value;

        /// <inheritdoc />
        public override string ToString() => $"{Value}";
    }

    /// <summary></summary>
    public enum PushNotificationTagTarget 
    {
        /// <summary>User</summary>
        User,
        /// <summary>Device</summary>
        Device
    }
}
