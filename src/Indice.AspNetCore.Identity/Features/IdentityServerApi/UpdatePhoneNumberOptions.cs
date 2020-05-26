using Indice.Services;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Options for the SMS sent when a user updates his phone number.
    /// </summary>
    public class UpdatePhoneNumberOptions
    {
        /// <summary>
        /// Controls whether an SMS is sent to the user when the phone number is updated, containing an OTP token, or not. Defaults to false.
        /// </summary>
        /// <remarks>Hint: also remember to register an implementation of <see cref="ISmsService"/>.</remarks>
        public bool SendOtpOnUpdate { get; set; }
    }
}
