namespace Indice.Types
{
    /// <summary></summary>
    public readonly struct PushNotificationTag
    {
        /// <summary>Creates a new instance of <see cref="PushNotificationTag"/>, providing the <paramref name="value"/> and <paramref name="refersTo"/> parameters..</summary>
        /// <param name="value">The value of the tag.</param>
        /// <param name="refersTo">Indicates whether the tag refers to the user or the device.</param>
        public PushNotificationTag(string value, PushNotificationTagReferral refersTo) {
            Value = value ?? throw new System.ArgumentNullException(nameof(value));
            RefersTo = refersTo;
        }

        /// <summary>Creates a new instance of <see cref="PushNotificationTag"/>, providing the <paramref name="value"/> and parameter and use <see cref="PushNotificationTagReferral.User"/> as default <see cref="RefersTo"/>.</summary>
        /// <param name="value">The value of the tag.</param>
        public PushNotificationTag(string value) : this(value, PushNotificationTagReferral.User) { }

        /// <summary>The value of the tag.</summary>
        public string Value { get; }
        /// <summary>Indicates whether the tag refers to the user or the device.</summary>
        public PushNotificationTagReferral RefersTo { get; }

        /// <summary>Implicit conversion of <see cref="PushNotificationTag"/> to string.</summary>
        /// <param name="tag">The <see cref="PushNotificationTag"/> instance.</param>
        public static implicit operator string(PushNotificationTag tag) => tag.Value;

        /// <inheritdoc />
        public override string ToString() => $"{Value}";
    }

    /// <summary></summary>
    public enum PushNotificationTagReferral 
    {
        /// <summary>User</summary>
        User,
        /// <summary>Device</summary>
        Device
    }
}
