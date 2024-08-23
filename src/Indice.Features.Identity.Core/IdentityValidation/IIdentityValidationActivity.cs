using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Core.IdentityValidation;

/// <summary>
/// Represents an activity that will take place after the successful first authentication of the user 
/// but before releasing and completing the signin process. 
/// These activities are meant for the user to complete any pending tasks regarding his identity so that after
/// The signin process completes we end up with a valid identity.
/// Examples of such activities are MFA, EmailConfirmation, PhoneConfirmation, PasswordChange, MFAOnBoarding, TermsAcceptance.
/// </summary>
/// <remarks>These activities will determine the flow that the user must take. They are implemented as a pipeline of filters.</remarks>
public interface IIdentityValidationActivity
{
    /// <summary>
    /// The next activity in the pipeline.
    /// </summary>
    public IIdentityValidationActivity? Next { get; set; }
    /// <summary>
    /// When called the handle method will try to handle the incoming request. 
    /// If it is not meant for this Activity then it will pass it to the next.
    /// </summary>
    /// <param name="user">The current user state.</param>
    /// <returns></returns>
    Task HandleAsync(User user);
}

