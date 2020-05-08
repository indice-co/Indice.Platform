using Indice.Services;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Options for the SMS sent when a user updates his phone number.
    /// </summary>
    public class UpdatePhoneNumberOptions
    {
        /// <summary>
        /// Controls whether an SMS is sent to the user, containing an OTP token, or not. Defaults to false.
        /// </summary>
        /// <remarks>Hint: also remember to register an implementation of <see cref="ISmsService"/>.</remarks>
        public bool SendOtpOnUpdate { get; set; }
        /// <summary>
        /// The text message of the SMS.
        /// </summary>
        /// <remarks>The message must contain a placeholder, which is {token}, in order to put the actual value of the generated verification code.</remarks>
        public string Message { get; set; } = "SMS verification code is {token}.";
    }
}
