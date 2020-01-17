namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Options for the SMS sent when a user changes his phone number.
    /// </summary>
    public class ChangePhoneNumberOptions
    {
        /// <summary>
        /// The subject of the SMS.
        /// </summary>
        public string Subject { get; set; } = "Confirm your phone";
        /// <summary>
        /// The message to be sent vis SMS.
        /// </summary>
        public string Message { get; set; } = "Your code is {code}.";
    }
}
