using System.Globalization;
using Microsoft.AspNetCore.Html;


namespace Indice.Features.Identity.UI;
/// <summary>
/// Provides descriptive labels and messages for identity-related UI elements.
/// </summary>
public class IdentityUILocalizer
{
    /// <summary>Text for the email label.</summary>
    public virtual HtmlString Email => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Email));
    /// <summary>Text for select language label.</summary>
    public virtual HtmlString SelectLanguage => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.SelectLanguage));

    #region AcceptTerms
    /// <summary>Text for the Accept button on the Accept Terms page.</summary>
    public virtual HtmlString AcceptTerms_Accept => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AcceptTerms_Accept));

    /// <summary>Text for the Reject button on the Accept Terms page.</summary>
    public virtual HtmlString AcceptTerms_Reject => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AcceptTerms_Reject));

    /// <summary>Title of the Accept Terms page.</summary>
    public virtual HtmlString AcceptTerms_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AcceptTerms_PageTitle));

    /// <summary>Title of the Accept Terms header.</summary>
    public virtual HtmlString AcceptTerms_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AcceptTerms_PageHeader));

    /// <summary>Instruction message prompting the user to read and accept the terms and conditions.</summary>
    public virtual HtmlString AcceptTerms_ReadAndAcceptTerms => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AcceptTerms_ReadAndAcceptTerms));
    #endregion

    #region AddEmail
    /// <summary>Label for the Next button on the Add Email page, formatted with the provided email.</summary>
    public virtual HtmlString AddEmail_Next => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddEmail_Next));

    /// <summary>Label for the Save button on the Add Email page.</summary>
    public virtual HtmlString AddEmail_Save => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddEmail_Save));

    /// <summary>Instruction message prompting the user to verify their email.</summary>
    public virtual HtmlString AddEmail_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddEmail_PageTitle));

    /// <summary>Instruction message prompting the user to verify their email.</summary>
    public virtual HtmlString AddEmail_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddEmail_PageHeader));
    #endregion

    #region AddPassword
    /// <summary>Indicates that the password field is required.</summary>
    public virtual HtmlString AddPassword_Required => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPassword_Required));

    /// <summary>Label for the Add Password button.</summary>
    public virtual HtmlString AddPassword_Add => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPassword_Add));

    /// <summary>Label for the New Password input field.</summary>
    public virtual HtmlString AddPassword_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPassword_PageTitle));

    /// <summary>Label for the New Password input field (alternate reference).</summary>
    public virtual HtmlString AddPassword_Newpassword_FieldLabel => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPassword_Newpassword_FieldLabel));

    /// <summary>Label for the Confirm Password input field.</summary>
    public virtual HtmlString AddPassword_ConfirmPassword => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPassword_PasswordConfirmation));

    /// <summary>Success message displayed when the password has been successfully added.</summary>
    public virtual HtmlString AddPassword_PasswordSuccessfullyAdded => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPassword_PasswordSuccessfullyAdded));

    /// <summary>Message indicating that the password addition process has been completed.</summary>
    public virtual HtmlString AddPassword_ProcessCompleted => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPassword_ProcessCompleted));
    #endregion


    #region AddPhone
    /// <summary>Label for the Add Phone action button.</summary>
    public virtual HtmlString AddPhone_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPhone_pageTitle));
    /// <summary>Title of the Add Phone page.</summary>
    public virtual HtmlString AddPhone_AddPhoneNumber => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPhone_AddPhoneNumber));
    /// <summary>Instruction message indicating the calling code selection for the phone number.</summary>
    public virtual HtmlString AddPhone_CallingCode => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPhone_CallingCode));
    /// <summary>Label for the Phone Number input field.</summary>
    public virtual HtmlString AddPhone_PhoneNumber => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPhone_PhoneNumber));
    /// <summary>Label for the Save button on the Add Phone page.</summary>
    public virtual HtmlString AddPhone_Save => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPhone_Save));
    #endregion

    #region Challenge

    /// <summary>
    /// Message shown when redirecting during an authentication challenge.
    /// </summary>
    public virtual HtmlString Challenge_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Challenge_PageTitle));

    #endregion

    #region ChangePassword
    /// <summary>Label for Sign in with different Account.</summary>
    public virtual HtmlString ChangePassword_SignWithDifferentAccount => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ChangePassword_SignWithDifferentAccount));
    /// <summary>Label for the Change Password action button.</summary>
    public virtual HtmlString ChangePassword_Change => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ChangePassword_Change));
    /// <summary>Label for the Current Password input field.</summary>
    public virtual HtmlString ChangePassword_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ChangePassword_PageTitle));
    /// <summary>HEader of page ChangePassword field.</summary>
    public virtual HtmlString ChangePassword_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ChangePassword_PageHeader));


    /// <summary>Label for the New Password input field.</summary>
    public virtual HtmlString ChangePassword_Newpassword_FieldLabel => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ChangePassword_Newpassword_FieldLabel));
    /// <summary>Label for the Old Password input field.</summary>
    public virtual HtmlString ChangePassword_OldPassword => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ChangePassword_OldPassword_FieldLabel));
    /// <summary>Success message displayed when the password has been successfully changed.</summary>
    public virtual HtmlString ChangePassword_PasswordSuccessfullyChanged => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ChangePassword_PasswordSuccessfullyChanged));
    /// <summary>Message displayed when the password change process is completed.</summary>
    public virtual HtmlString ChangePassword_ProcessCompleted => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ChangePassword_ProcessCompleted));
    #endregion


    #region ConfirmEmail
    /// <summary>Instruction not to reply to service emails.</summary>
    public virtual HtmlString ConfirmEmail_DoNotReply => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_DoNotReply));
    /// <summary>Info that the email contains account-related information.</summary>
    public virtual HtmlString ConfirmEmail_ServiceEmailInfo => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_ServiceEmailInfo));
    /// <summary>Text for the Confirm Email button.</summary>
    public virtual HtmlString ConfirmEmail_Click => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_Click));
    /// <summary>Title of the Confirm Email page.</summary>
    public virtual HtmlString ConfirmEmail_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_PageTitle));
    /// <summary>Error message displayed when the email verification link has expired.</summary>
    public virtual HtmlString ConfirmEmail_Email_Verification_Link_Has_Expired => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_LinkExpired));
    /// <summary>Instruction message guiding the user on the Confirm Email page.</summary>
    public virtual HtmlString ConfirmEmail_Here => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_here));
    /// <summary>Instruction displayed if the user received the error by mistake.</summary>
    public virtual HtmlString ConfirmEmail_If_You_Feel_You_Got_This_Error_By_Mistake_Simply_Click => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_MistakeText));
    /// <summary>Error message displayed when no action was taken.</summary>
    public virtual HtmlString ConfirmEmail_No_Action_Was_Taken => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_NoActionTaken));
    /// <summary>Instruction displayed when the user needs to log in and resend the verification link.</summary>
    public virtual HtmlString ConfirmEmail_Please_Log_In_And_Resend_The_Link => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_LoginResendText));
    /// <summary>Instruction message indicating that verification may take a moment and the user should click the link below.</summary>
    public virtual HtmlString ConfirmEmail_This_May_Take_A_Moment_Click_On_The_Link_Below_To_Verify_Your_Email_Address => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_ClickToVerify));
    /// <summary>Text for navigating back to sign in or return to the previous page.</summary>
    public virtual HtmlString ConfirmEmail_To_Sign_In_Or_Go_Back => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_GoBackToSignIn));
    /// <summary>Label for the Verify action button.</summary>
    public virtual HtmlString ConfirmEmail_Verify => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_Verify));
    /// <summary>Instruction displayed while verifying the user's email.</summary>
    public virtual HtmlString ConfirmEmail_Verifying_Your_Email => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_VerifyingEmail));
    /// <summary>Instruction indicating the user can close the browser window after verification.</summary>
    public virtual HtmlString ConfirmEmail_You_Can_Now_Close_This_Browse_Window => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_Closewindow));
    /// <summary>Success message displayed when the email has been successfully confirmed.</summary>  
    public virtual HtmlString ConfirmEmail_Your_Email_Has_Been_Successfully_Confirmed => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_Confrimation));
    #endregion

    #region ConfirmEmailChange
    /// <summary>Text for the Confirm Email Change button.</summary>
    public virtual HtmlString ConfirmEmailChange_Click => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_Click));
    /// <summary>Title of the Confirm Email Change page.</summary>
    public virtual HtmlString ConfirmEmailChange_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_PageTitle));
    /// <summary>Error message displayed when the email verification link has expired.</summary>
    public virtual HtmlString ConfirmEmailChange_Email_Verification_Link_Has_Expired => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_LinkExpired));
    /// <summary>Instruction message guiding the user on the Confirm Email Change page.</summary>
    public virtual HtmlString ConfirmEmailChange_Here => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_here));
    /// <summary>Instruction displayed if the user received the error by mistake.</summary>
    public virtual HtmlString ConfirmEmailChange_If_You_Feel_You_Got_This_Error_By_Mistake_Simply_Click => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_MistakeText));
    /// <summary>Error message displayed when no action was taken.</summary>
    public virtual HtmlString ConfirmEmailChange_No_Action_Was_Taken => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_NoAction));
    /// <summary>Instruction displayed when the user needs to log in and resend the verification link.</summary>
    public virtual HtmlString ConfirmEmailChange_Please_Log_In_And_Resend_The_Link => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_LoginResendText));
    /// <summary>Instruction message indicating that verification may take a moment and the user should click the link below.</summary>
    public virtual HtmlString ConfirmEmailChange_This_May_Take_A_Moment_Click_On_The_Link_Below_To_Verify_Your_Email_Address => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_VerificationText));
    /// <summary>Text for navigating back to sign in or return to the previous page.</summary>
    public virtual HtmlString ConfirmEmailChange_To_Sign_In_Or_Go_Back => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_SignInGoBack));
    /// <summary>Label for the Verify action button.</summary>
    public virtual HtmlString ConfirmEmailChange_Verify => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_Verify));
    /// <summary>Instruction displayed while verifying the user's email.</summary>
    public virtual HtmlString ConfirmEmailChange_Verifying_Your_Email => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_VerifyingEmail));
    /// <summary>Instruction indicating the user can close the browser window after verification.</summary>
    public virtual HtmlString ConfirmEmailChange_You_Can_Now_Close_This_Browse_Window => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_CloseWindowMessage));
    /// <summary>Success message displayed when the email has been successfully changed.</summary>
    public virtual HtmlString ConfirmEmailChange_Your_Email_Has_Been_Successfully_Changed => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_ConfirmationEmailChanged));
    /// <summary>Message displayed when the email is already verified.</summary>
    public virtual HtmlString ConfirmEmailChange_Your_Email_Is_Already_Verified_Thank_You => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_ConfirmationVerified));
    #endregion


    #region Consent
    /// <summary>Gets the label text for the Accept button on the Consent page.</summary>
    public virtual HtmlString Consent_Accept => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_Accept));
    /// <summary>Gets the label text displaying the application name.</summary>
    public virtual HtmlString Consent_App => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_app));
    /// <summary>Gets the label for the Application Access section.</summary>
    public virtual HtmlString Consent_Application_Access => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_ApplicationAccess));
    /// <summary>Gets the label text for the Cancel action.</summary>
    public virtual HtmlString Consent_Cancel => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_Cancel));
    /// <summary>Gets the label text for the Code input field.</summary>
    public virtual HtmlString Consent_Code => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_code));
    /// <summary>Gets the label text for the Consent action.</summary>
    public virtual HtmlString Consent_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_PageTitle));
    /// <summary>Gets the label text for personal information section.</summary>
    public virtual HtmlString Consent_Personal_Information => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_PersonalInformation));
    /// <summary>Gets the label text for the Remember My Decision checkbox.</summary>
    public virtual HtmlString Consent_Remember_My_Decision => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_RememberMyDecision));
    /// <summary>Gets the label text for the Security Code input field.</summary>
    public virtual HtmlString Consent_Security_Code => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_SecurityCode));
    /// <summary>Gets the label text for the Send action.</summary>
    public virtual HtmlString Consent_Send => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_send));
    /// <summary>Gets the label text for the Permissions section title.</summary>
    public virtual HtmlString Consent_This => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_This));
    /// <summary>Gets the text indicating what the current app would like to access.</summary>
    public virtual HtmlString Consent_This_App_Would_Like_To => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_ThisAppWouldLikeTo));
    /// <summary>Gets the text describing access to the user's phone.</summary>
    public virtual HtmlString Consent_To_Your_Phone => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_ToYourPhone));
    /// <summary>Gets the instruction to uncheck permissions the user does not wish to grant.</summary>
    public virtual HtmlString Consent_Uncheck_The_Permissions_You_Do_Not_Wish_To_Grant => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_UncheckPermissions));
    /// <summary>Gets the text indicating the app’s request for permission.</summary>
    public virtual HtmlString Consent_Would_Like_To => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_WouldLikeTo));
    #endregion

    #region Email

    /// <summary>Preheader text prompting the user to verify their email.</summary>
    public virtual HtmlString Email_Preheader_Verify => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Email_Preheader_Verify));

    /// <summary>Text for the "here" link in email preheader.</summary>
    public virtual HtmlString Email_Preheader_Here => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Email_Preheader_Here));

    /// <summary>Text following the "here" link in preheader.</summary>
    public virtual HtmlString Email_Preheader_ToGetVerified => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Email_Preheader_ToGetVerified));

    /// <summary>Greeting text at the beginning of the email body.</summary>
    public virtual HtmlString Email_Body_Hi => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Email_Body_Hi));

    /// <summary>Text for "Thanks" in the email body.</summary>
    public virtual HtmlString Email_Body_Thanks => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Email_Body_Thanks));

    /// <summary>Text for "Sincerely" in the email body.</summary>
    public virtual HtmlString Email_Body_Sincerely => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Email_Body_Sincerely));

    /// <summary>Organization name placeholder in email body.</summary>
    public virtual HtmlString Email_Body_Organization(string organization) => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Email_Body_Organization, organization));
    /// <summary>
    /// Label for the Reset password action.
    /// </summary>
    public virtual HtmlString Email_Reset_Password => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Email_Reset_Password_FieldLabel));
    /// <summary>
    /// Text informing the user about the password reset request.
    /// </summary>
    public virtual HtmlString Email_ResetPassword_RequestInfo => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Email_ResetPassword_RequestInfo));

    /// <summary>
    /// Text informing the user they can ignore the email if they didn't request a reset.
    /// </summary>
    public virtual HtmlString Email_ResetPassword_IgnoreNotice => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Email_ResetPassword_IgnoreNotice));


    #endregion

    #region EmailRegister
    /// <summary>Email subject: "Welcome to {0}! Please confirm your email to get started."</summary>
    public virtual HtmlString EmailRegister_Subject(string appName) => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailRegister_Subject, appName));

    /// <summary>Body text: "Welcome to {0}! We’re excited to have you on board."</summary>
    public virtual HtmlString EmailRegister_Body_Welcome(string appName) => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailRegister_Body_Welcome, appName));

    /// <summary>Body text prompting the user to confirm their email.</summary>
    public virtual HtmlString EmailRegister_Body_ConfirmEmail => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailRegister_Body_ConfirmEmail));

    /// <summary>Body text telling user they can ignore if they didn’t create account.</summary>
    public virtual HtmlString EmailRegister_Body_IgnoreNotice => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailRegister_Body_IgnoreNotice));

    /// <summary>Body text: "Thank you for joining!"</summary>
    public virtual HtmlString EmailRegister_Body_Thanks => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailRegister_Body_Thanks));
    /// <summary>Greeting used at the beginning of the registration email.</summary>
    public virtual HtmlString EmailRegister_Body_Hi => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailRegister_Body_Hi));

    #endregion

    #region EmailSecurity

    /// <summary>Preheader of the security notification email.</summary>
    public virtual HtmlString EmailSecurity_Preheader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailSecurity_Preheader));

    /// <summary>Subject of the security notification email.</summary>
    public virtual HtmlString EmailSecurity_Subject => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailSecurity_Subject));

    /// <summary>Greeting "Hi" in the security notification email.</summary>
    public virtual HtmlString EmailSecurity_Body_Hi => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailSecurity_Body_Hi));

    /// <summary>Message that a security event occurred on the account.</summary>
    public virtual HtmlString EmailSecurity_Body_EventOccurred => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailSecurity_Body_EventOccurred));

    /// <summary>Label for event details section.</summary>
    public virtual HtmlString EmailSecurity_Body_EventDetails => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailSecurity_Body_EventDetails));

    /// <summary>Label for event time field.</summary>
    public virtual HtmlString EmailSecurity_Body_EventTime => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailSecurity_Body_EventTime));

    /// <summary>Label for username field.</summary>
    public virtual HtmlString EmailSecurity_Body_Username => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailSecurity_Body_Username));

    /// <summary>Label for email field.</summary>
    public virtual HtmlString EmailSecurity_Body_Email => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailSecurity_Body_Email));

    /// <summary>Label for device field.</summary>
    public virtual HtmlString EmailSecurity_Body_Device => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailSecurity_Body_Device));

    /// <summary>Label for client field.</summary>
    public virtual HtmlString EmailSecurity_Body_Client => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailSecurity_Body_Client));

    /// <summary>Label for location field.</summary>
    public virtual HtmlString EmailSecurity_Body_Location => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailSecurity_Body_Location));

    /// <summary>Notice about approximate location.</summary>
    public virtual HtmlString EmailSecurity_Body_LocationNotice => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailSecurity_Body_LocationNotice));

    /// <summary>Message advising the user to contact support if the action was not theirs.</summary>
    public virtual HtmlString EmailSecurity_Body_ContactSupport => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.EmailSecurity_Body_ContactSupport));

    #endregion

    #region Error
    /// <summary>General label for error messages.</summary>
    public virtual HtmlString Error_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Error_PageTitle));
    /// <summary>Header label for error pages.</summary>
    public virtual HtmlString Error_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Error_PageHeader));
    /// <summary> Label for the "Go back home" action, typically shown on error pages. </summary>
    public virtual HtmlString Error_Go_back_home => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Error_Go_back_home));

    /// <summary>Gets the title text for the Error page.</summary>
    public virtual HtmlString Error_Oops_Seems_We_Encountered_An_Error => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Error_Oops));
    /// <summary>Gets the label text displaying the request ID for error tracking.</summary>
    public virtual HtmlString Error_Request_Id => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Error_RequestId));
    #endregion

    #region ForgotPassword
    /// <summary>Gets the title text for the Forgot Password page.</summary>
    public virtual HtmlString ForgotPassword_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPassword_PageTitle));
    /// <summary>Gets the message indicating that a password reset request has been sent successfully.</summary>
    public virtual HtmlString ForgotPassword_Request_Sent => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPassword_RequestSent));
    /// <summary>Gets the label text for resending the password reset email.</summary>
    public virtual HtmlString ForgotPassword_Resend => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPassword_Resend));
    /// <summary>Gets the label text for the Send button on the Forgot Password page.</summary>
    public virtual HtmlString ForgotPassword_Send => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPassword_Send));
    /// <summary>Gets the instructional text guiding the user to enter their username or email to receive a password reset link.</summary>
    public virtual HtmlString ForgotPassword_ResetInstructions => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPassword_ResetInstructions));
    /// <summary>Gets the label text for the Email or Username input field.</summary>
    public virtual HtmlString ForgotPassword_Username => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPassword_Username));
    #endregion

    #region Footer

    #region Footer

    /// <summary>Footer link to discovery document (visible only in non-production).</summary>
    public virtual HtmlString Footer_Discovery => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Footer_Discovery));

    /// <summary>Footer link text for privacy policy.</summary>
    public virtual HtmlString Footer_Privacy => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Footer_Privacy));

    /// <summary>Footer link text for terms of service.</summary>
    public virtual HtmlString Footer_Terms => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Footer_Terms));

    /// <summary>Footer link text for contact page.</summary>
    public virtual HtmlString Footer_Contact_us => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Footer_Contact_us));

    #endregion

    #endregion

    #region ForgotPasswordConfirmation
    /// <summary>Gets the title text for the Forgot Password Confirmation page.</summary>
    public virtual HtmlString ForgotPasswordConfirmation_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPasswordConfirmation_PageTitle));
    /// <summary>Gets the instruction text prompting the user to enter a new password.</summary>
    public virtual HtmlString ForgotPasswordConfirmation_New_Password => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPasswordConfirmation_Newpassword_FieldLabel));
    /// <summary>Gets the message indicating that the password has been successfully changed.</summary>
    public virtual HtmlString ForgotPasswordConfirmation_Password_Changed => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPasswordConfirmation_PasswordChanged));
    /// <summary>Gets the instruction text asking the user to log in using their new password. Includes a link for login.</summary>
    /// <param name="callbackUrl">The URL to the login page.</param>
    public virtual HtmlString ForgotPasswordConfirmation_LoginLink(string callbackUrl) => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPasswordConfirmation_LoginLink, callbackUrl));
    /// <summary>Gets the label prompting the user to fill in their new password.</summary>
    public virtual HtmlString ForgotPasswordConfirmation_Please_Fill_In_Your_New_Password => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPasswordConfirmation_FillNewPassword_FieldLabel));
    /// <summary>Gets the text for the "Send" button on the Forgot Password Confirmation page.</summary>
    public virtual HtmlString ForgotPasswordConfirmation_Send => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPasswordConfirmation_Send));
    #endregion

    #region Grants
    /// <summary>Gets the header text for the Grants page listing authorized applications and resources.</summary>
    public virtual HtmlString Grants_Applications_And_Resources => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Grants_ListApplicationsGrants));
    /// <summary>Gets the label text for the client logo.</summary>
    public virtual HtmlString Grants_Client_Logo => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Grants_ClientLogo));
    /// <summary>Gets the label text indicating when a grant was created.</summary>
    public virtual HtmlString Grants_Created => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Grants_Created));
    /// <summary>Gets the label text indicating when a grant expires.</summary>
    public virtual HtmlString Grants_Expires_On => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Grants_ExpiresOn));
    /// <summary>Gets the label text for the grants section.</summary>
    public virtual HtmlString Grants_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Grants_PageTitle));
    /// <summary>Gets the label text for the grants header.</summary>
    public virtual HtmlString Grants_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Grants_PageHeader));

    /// <summary>Gets the text for the action to revoke access from an application or resource.</summary>
    public virtual HtmlString Grants_Revoke_Access => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Grants_RevokeAccess));
    /// <summary>Gets the message displayed when no applications have been granted access.</summary>
    public virtual HtmlString Grants_No_Applications_Provided => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Grants_NoAccessGiven));
    #endregion


    #region Home
    /// <summary>Title of the Home page.</summary>
    public virtual HtmlString Home_Welcome_Portal_Of(string portalName) => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_WelcomeMessage, portalName));
    /// <summary>Instruction message on the Home page.</summary>
    public virtual HtmlString Home_Authorized_Applications => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_authorizedapplications));
    /// <summary>Instruction message on the Home page.</summary>
    public virtual HtmlString Home_Check_And_Revoke_Your => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_CheckAndRevoke));
    /// <summary>Instruction message on the Home page.</summary>
    public virtual HtmlString Home_Digital_Services => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_DigitalServices));
    /// <summary>Instruction message on the Home page.</summary>
    public virtual HtmlString Home_Here => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_here));
    /// <summary>Instruction message on the Home page.</summary>
    public virtual HtmlString Home_Manage_Your_Grants => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_ManageGrants));
    /// <summary>Instruction message on the Home page.</summary>
    public virtual HtmlString Home_Please_Login_To_The_Application => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_PleaseLogin));
    /// <summary>Instruction message on the Home page.</summary>
    public virtual HtmlString Home_Portal => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_Portal));
    /// <summary>Instruction message on the Home page.</summary>
    public virtual HtmlString Home_IntroTextWithOrgName(string serviceName) => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_IntroTextWithOrgName, serviceName));
    /// <summary>Instruction message on the Home page.</summary>
    public virtual HtmlString Home_Welcome => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_Welcome));
    /// <summary>Instruction message on the Home page.</summary>
    public virtual HtmlString Home_Welcome_Back => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_WelcomeBack));
    /// <summary>Instruction message on the Home page.</summary>
    public virtual HtmlString Home_WelcomeDigitalServices(string serviceName) => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_WelcomeDigitalServices, serviceName));
    #endregion

    #region LoggedOut
    /// <summary>Title of the Logged Out page.</summary>
    public virtual HtmlString LoggedOut_Click => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.LoggedOut_Click));
    /// <summary>Instruction message on the Logged Out page.</summary>
    public virtual HtmlString LoggedOut_Here => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.LoggedOut_here));
    /// <summary>Instruction message on the Logged Out page.</summary>
    public virtual HtmlString LoggedOut_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.LoggedOut_PageTitle));
    /// <summary>Instruction message on the Logged Out page.</summary>
    public virtual HtmlString LoggedOut_To_Return_To_The_Application => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.LoggedOut_ReturnToApplication));
    /// <summary>Success message when the user has been logged out.</summary>
    public virtual HtmlString LoggedOut_You_Are_Now_Logged_Out => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.LoggedOut_YouAreLoggedOut));
    #endregion

    #region Login

    /// <summary>
    ///   Gets the localized string for "Don't have an account?" on the Login page.
    /// </summary>
    public virtual HtmlString Login_Dont_Have_An_Account => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_NoAccount));

    /// <summary>
    ///   Gets the localized string for "Forgot Password_FieldLabel" instruction on the Login page.
    /// </summary>
    public virtual HtmlString Login_Forgot_Password => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_ForgotPassword_FieldLabel));

    /// <summary>
    ///   Gets the localized string for "Invalid login request" instruction on the Login page.
    /// </summary>
    public virtual HtmlString Login_Invalid_Login_Request => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_InvalidLoginRequest));

    /// <summary>
    ///   Gets the localized string for "Join Us" instruction on the Login page.
    /// </summary>
    public virtual HtmlString Login_Join_Us => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_JoinUs));

    /// <summary>
    ///   Gets the localized string for "Login" label.
    /// </summary>
    public virtual HtmlString Login_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_PageTitle));

    /// <summary>
    ///   Gets the localized string for "OR" label.
    /// </summary>
    public virtual HtmlString Login_OR => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_OR));

    /// <summary>
    ///   Gets the localized string for "Password_FieldLabel" label.
    /// </summary>
    public virtual HtmlString Login_Password => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_Password_FieldLabel));

    /// <summary>
    ///   Gets the localized string for "Remember me" option.
    /// </summary>
    public virtual HtmlString Login_Remember_Me => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_RememberMe));

    /// <summary>
    ///   Gets the localized string for "Sign In" button.
    /// </summary>
    public virtual HtmlString Login_Sign_In => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_SignIn));

    /// <summary>
    ///   Gets the localized string for "No login schemes configured" message.
    /// </summary>
    public virtual HtmlString Login_No_Login_Schemes_Configured => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_NoLoginSchemes));

    /// <summary>
    ///   Gets the localized string for "Username" label.
    /// </summary>
    public virtual HtmlString Login_Username => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_Username));

    #endregion

    #region Logout

    /// <summary>
    ///   Gets the localized string for "Logout" button.
    /// </summary>
    public virtual HtmlString Logout_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Logout_PageTitle));

    /// <summary>
    ///   Gets the localized string for the confirmation message "Would you like to logout?".
    /// </summary>
    public virtual HtmlString Logout_Would_You_Like_To_Logout => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Logout_WantToLogout));

    /// <summary>
    ///   Gets the localized string for "Yes" button on the logout confirmation dialog.
    /// </summary>
    public virtual HtmlString Logout_Yes => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Logout_Yes));

    #endregion



    #region Mfa

    /// <summary>
    ///   Gets the localized string for "An unexpected error occurred while sending the OTP code".
    /// </summary>
    public virtual HtmlString Mfa_An_Unexpected_Error_Occurred_While_Sending_The_OTP_Code => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_UnexpectedErrorSendingOTPCode));

    /// <summary>
    ///   Gets the localized string for "Authenticate".
    /// </summary>
    public virtual HtmlString Mfa_Authenticate => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_Authenticate));

    /// <summary>
    ///   Gets the localized string for "Authenticate yourself using...".
    /// </summary>
    public virtual HtmlString Mfa_Authenticate_Yourself_Using => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_AuthenticateUsing));

    /// <summary>
    ///   Gets the localized string for "Back".
    /// </summary>
    public virtual HtmlString Mfa_Back => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_Back));

    /// <summary>
    ///   Gets the localized string for "Because you've turned on two-step verification, you need to approve request on your mobile app".
    /// </summary>
    public virtual HtmlString Mfa_Because_Youve_Turned_On_Two_Step_Verification_You_Need_To_Approve_Request_On_Your_Mobile_App => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_YouNeedToApproveRequest));

    /// <summary>
    ///   Gets the localized string for "I can't use my app right now".
    /// </summary>
    public virtual HtmlString Mfa_I_Cant_Use_My_App_Right_Now => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_CannotUseAppNow));

    /// <summary>
    ///   Gets the localized string for "I didn't receive the notification. Resend".
    /// </summary>
    public virtual HtmlString Mfa_I_Didnt_Receive_The_Notification_Resend => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_DidnotReceiveNotification));

    /// <summary>
    ///   Gets the localized string for "I sign in frequently here. Remember this browser".
    /// </summary>
    public virtual HtmlString Mfa_I_Sign_In_Frequently_Here_Remember_This_Browser => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_ISignInFrequently));

    /// <summary>
    ///   Gets the localized string for "Login".
    /// </summary>
    public virtual HtmlString Mfa_Login => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_Login));

    /// <summary>
    ///   Gets the localized string for "Multifactor Authentication".
    /// </summary>
    public virtual HtmlString Mfa_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_PageTitle));

    /// <summary>
    ///   Gets the localized string for "Other authentication methods...".
    /// </summary>
    public virtual HtmlString Mfa_Other_Authentication_Methods => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_OtherAuthenticationMethods));

    /// <summary>
    ///   Gets the localized string for "OTP Code".
    /// </summary>
    public virtual HtmlString Mfa_OTP_Code => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_OTPCode));

    /// <summary>
    ///   Gets the localized string for "Request denied".
    /// </summary>
    public virtual HtmlString Mfa_Request_Denied => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_RequestDenied));

    /// <summary>
    ///   Gets the localized string for "Resend".
    /// </summary>
    public virtual HtmlString Mfa_Resend => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_Resend));

    /// <summary>
    ///   Gets the localized string for "Send another request to my app".
    /// </summary>
    public virtual HtmlString Mfa_Send_Another_Request_To_My_App => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_SendAnotherRequestToApp));

    /// <summary>
    ///   Gets the localized string for "We sent an identity verification request to your mobile device, but you denied it".
    /// </summary>
    public virtual HtmlString Mfa_IdentityVerificationWasDenied => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_IdentityVerificationWasDenied));

    /// <summary>
    ///   Gets the localized string for "We texted your phone {0}. Please enter the code to sign in" with a phone number parameter.
    /// </summary>
    public virtual HtmlString Mfa_We_Texted_Your_Phone(string phoneNumber) => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_WeTextedYourPhone, phoneNumber));

    #endregion


    #region MfaModel

    /// <summary>
    ///   Gets the localized string for "OTP login".
    /// </summary>
    public virtual HtmlString MfaModel_OTP_Login => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaModel_OTPLogin));

    /// <summary>
    ///   Gets the localized string for "The OTP code is not valid."
    /// </summary>
    public virtual HtmlString MfaModel_The_OTP_Code_Is_Not_Valid => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaModel_TheOTPIsInvalid));

    /// <summary>
    ///   Gets the localized string for "Your OTP code for login is: {0}" with a code parameter.
    /// </summary>
    public virtual HtmlString MfaModel_Your_OTP_Code_For_Login(string code) => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaModel_OTPCodeMessage, code));

    #endregion

    #region MfaOnBoarding

    /// <summary>
    ///   Gets the localized string for "Enable MFA".
    /// </summary>
    public virtual HtmlString MfaOnBoarding_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoarding_PageTitle));
    /// <summary>
    ///   Gets the localized string for "Enable MFA".
    /// </summary>
    public virtual HtmlString MfaOnboarding_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoarding_PageHeader));

    /// <summary>
    ///   Gets the localized string for "Keep your account safe".
    /// </summary>
    public virtual HtmlString MfaOnBoarding_Keep_Your_Account_Safe => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoarding_KeepYourAccountSafe));

    /// <summary>
    ///   Gets the localized string for "Next".
    /// </summary>
    public virtual HtmlString MfaOnBoarding_Next => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoarding_Next));

    /// <summary>
    ///   Gets the localized string for "Setup an additional authentication method".
    /// </summary>
    public virtual HtmlString MfaOnBoarding_Setup_An_Additional_Authentication_Method => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoarding_SetupAdditionalAuthenticationMethod));

    #endregion

    #region MfaOnBoardingAddPhone

    /// <summary>
    ///   Gets the localized string for "MFA onboarding - SMS".
    /// </summary>
    public virtual HtmlString MfaOnBoardingAddPhone_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingAddPhone_PageTitle));

    /// <summary>
    ///   Gets the localized string for "Next".
    /// </summary>
    public virtual HtmlString MfaOnBoardingAddPhone_Next => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingAddPhone_Next));

    /// <summary>
    ///   Gets the localized string for "Phone number".
    /// </summary>
    public virtual HtmlString MfaOnBoardingAddPhone_Phone_Number => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingAddPhone_PhoneNumber));

    /// <summary>
    ///   Gets the localized string for "Save".
    /// </summary>
    public virtual HtmlString MfaOnBoardingAddPhone_Save => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingAddPhone_Save));

    #endregion



    #region MfaOnBoardingVerifyPhone

    /// <summary>
    ///   Gets the localized string for "Code".
    /// </summary>
    public virtual HtmlString MfaOnBoardingVerifyPhone_Code => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingVerifyPhone_Code));

    /// <summary>
    ///   Gets the localized string for "Next".
    /// </summary>
    public virtual HtmlString MfaOnBoardingVerifyPhone_Next => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingVerifyPhone_Next));

    /// <summary>
    ///   Gets the localized string for "Save".
    /// </summary>
    public virtual HtmlString MfaOnBoardingVerifyPhone_Save => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingVerifyPhone_Save));

    /// <summary>
    ///   Gets the localized string for "Verify phone number".
    /// </summary>
    public virtual HtmlString MfaOnBoardingVerifyPhone_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingVerifyPhone_PageTitle));

    #endregion

    #region PasswordExpired

    /// <summary>
    ///   Gets the localized string for "Change Password_FieldLabel".
    /// </summary>
    public virtual HtmlString PasswordExpired_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.PasswordExpired_PageTitle));

    /// <summary>
    ///   Gets the localized string for "New Password_FieldLabel".
    /// </summary>
    public virtual HtmlString PasswordExpired_New_Password => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.PasswordExpired_Newpassword_FieldLabel));

    /// <summary>
    ///   Gets the localized string for "Next".
    /// </summary>
    public virtual HtmlString PasswordExpired_Next => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.PasswordExpired_Next));

    /// <summary>
    ///   Gets the localized string for "Save".
    /// </summary>
    public virtual HtmlString PasswordExpired_Save => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.PasswordExpired_Save));

    /// <summary>
    ///   Gets the localized string for "New password confirmation".
    /// </summary>
    public virtual HtmlString PasswordExpired_New_Password_Confirmation => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.PasswordExpired_ΝewPasswordConfirmation));

    #endregion


    #region Profile

    /// <summary>
    /// Label for the Preferred Language field in profile forms.
    /// </summary>
    public virtual HtmlString Profile_PreferredLanguage => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_PreferredLanguage));

    /// <summary>
    ///   Gets the localized string for "A confirmation email has been sent to {0}.".
    /// </summary>
    public virtual HtmlString Profile_A_Confirmation_Email_Has_Been_Sent_To(string email) => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_ConfirmationEmailSentTo, email));

    /// <summary>
    /// Label for Manage Profile action.
    /// </summary>
    public virtual HtmlString Profile_Manage_Profile => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_ManageProfile));

    /// <summary>
    /// Placeholder text for dropdowns or selection inputs in profile forms.
    /// </summary>
    public virtual HtmlString Profile_Choose => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Choose));

    /// <summary>
    /// Email label in profile forms.
    /// </summary>
    public virtual HtmlString Profile_Email => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Email));

    /// <summary>
    /// Label for timezone selection in profile forms.
    /// </summary>
    public virtual HtmlString Profile_Timezone => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Timezone));

    /// <summary>
    ///   Gets the localized string for "An email has been sent to your new email address in order to confirm it.".
    /// </summary>
    public virtual HtmlString Profile_EmailSentToNewEmail => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_EmailSentToNewEmail));

    /// <summary>
    ///   Gets the localized string for "Birth date".
    /// </summary>
    public virtual HtmlString Profile_Birth_Date => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_BirthDate));

    /// <summary>
    ///   Gets the localized string for "Calling Code".
    /// </summary>
    public virtual HtmlString Profile_Calling_Code => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_CallingCode));

    /// <summary>
    ///   Gets the localized string for "Confirmation".
    /// </summary>
    public virtual HtmlString Profile_Confirmation => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Confirmation));

    /// <summary>
    ///   Gets the localized string for "Confirmation email delivery failed. Please contact system administrator.".
    /// </summary>
    public virtual HtmlString Profile_Confirmation_Email_Delivery_Failed_Please_Contact_System_Administrator => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_EmailDeliveryFailed));

    /// <summary>
    ///   Gets the localized string for "Connect a new provider".
    /// </summary>
    public virtual HtmlString Profile_Connect_A_New_Provider => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_ConnectNewProvider));

    /// <summary>
    ///   Gets the localized string for "Developer TOTP".
    /// </summary>
    public virtual HtmlString Profile_Developer_TOTP => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_DeveloperTOTP));

    /// <summary>
    ///   Gets the localized string for "Existing providers".
    /// </summary>
    public virtual HtmlString Profile_Existing_Providers => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_ExistingProviders));

    /// <summary>
    ///   Gets the localized string for "External providers".
    /// </summary>
    public virtual HtmlString Profile_External_Providers => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_ExternalProviders));

    /// <summary>
    ///   Gets the localized string for "First name".
    /// </summary>
    public virtual HtmlString Profile_First_Name => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_FirstName));

    /// <summary>
    ///   Gets the localized string for "here".
    /// </summary>
    public virtual HtmlString Profile_Here => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_here));

    /// <summary>
    ///   Gets the localized string for "I have been informed about the processing of my personal data and I consent to it, as specifically defined".
    /// </summary>
    public virtual HtmlString Profile_PrivacyPolicyConsent => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_InformAboutProcessing));

    /// <summary>
    ///   Gets the localized string for "Language".
    /// </summary>
    public virtual HtmlString Profile_Language => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Language));

    /// <summary>
    ///   Gets the localized string for "Last name".
    /// </summary>
    public virtual HtmlString Profile_Last_Name => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Lastname));

    /// <summary>
    ///   Gets the localized string for "Phone number".
    /// </summary>
    public virtual HtmlString Profile_Phone_Number => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_PhoneNumber));

    /// <summary>
    ///   Gets the localized string for "Preferences".
    /// </summary>
    public virtual HtmlString Profile_Preferences => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Preferences));

    /// <summary>
    ///   Gets the localized string for "Profile".
    /// </summary>
    public virtual HtmlString Profile_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_PageTitle));

    /// <summary>
    ///   Gets the localized string for "Remove".
    /// </summary>
    public virtual HtmlString Profile_Remove => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Remove));

    /// <summary>
    ///   Gets the localized string for "Save".
    /// </summary>
    public virtual HtmlString Profile_Save => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Save));

    /// <summary>
    ///   Gets the localized string for "Tax identification".
    /// </summary>
    public virtual HtmlString Profile_Tax_Identification => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_TaxIdentification));

    /// <summary>
    ///   Gets the localized string for "Unknown".
    /// </summary>
    public virtual HtmlString Profile_Unknown => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Unknown));

    /// <summary>
    ///   Gets the localized string for "Username".
    /// </summary>
    public virtual HtmlString Profile_Username => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Username));

    /// <summary>
    ///   Gets the localized string for "Your new email verification is still pending.".
    /// </summary>
    public virtual HtmlString Profile_Your_New_Email_Verification_Is_Still_Pending => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_VerificationPending));

    /// <summary>
    ///   Gets the localized string for "Your profile was updated successfully.".
    /// </summary>
    public virtual HtmlString Profile_Your_Profile_Was_Updated_Successfully => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_ProfileUpdatedSuccessfully));

    #endregion

    #region Redirect

    /// <summary>
    /// Text shown when the user is being returned to the application.
    /// </summary>
    public virtual HtmlString Redirect_Returning => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Redirect_Returning));

    /// <summary>
    /// Instruction shown to the user that the tab can be closed after redirection.
    /// </summary>
    public virtual HtmlString Redirect_CloseTab => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Redirect_CloseTab));

    #endregion

    #region Register

    /// <summary>
    /// Text for "Associate your" phrase.
    /// </summary>
    public virtual HtmlString Register_Associate_your => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Associate_your));

    /// <summary>
    /// Text for "account" phrase.
    /// </summary>
    public virtual HtmlString Register_account => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_account));

    /// <summary>
    /// Constructs the full "Associate your {Provider} account" message.
    /// </summary>
    /// <param name="provider">Name of the external provider (e.g., Google, Facebook).</param>
    public virtual HtmlString Register_Associate_your_Account(string provider) =>
        new HtmlString(
            string.Format(CultureInfo.CurrentUICulture, "{0} {1} {2}",
                IdentityLabels.Register_Associate_your,
                provider,
                IdentityLabels.Register_account));

    /// <summary>
    /// Text for "Register" button or label.
    /// </summary>
    public virtual HtmlString Register_Register => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Register));


    /// <summary>
    /// Text for "I have read and accept the Terms of service and privacy policy" on the Register page.
    /// </summary>
    /// <remarks>If you need to change link then prefer setting TermsUrl in IdentityUIOptions</remarks>
    public virtual HtmlString Register_I_Have_Read_And_Accept_Terms(string url = "/terms") => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_I_Have_Read_And_Accept_Terms, url));

    /// <summary>
    /// Instruction shown at the top of the registration form explaining what the user needs to do.
    /// </summary>
    public virtual HtmlString Register_Form_Instructions => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Form_Instructions));

    /// <summary>
    ///   Gets the localized string for "Already have an account?".
    /// </summary>
    public virtual HtmlString Register_Already_Have_An_Account => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_AlreadyHaveAccount));

    /// <summary>
    ///   Gets the localized string for "Calling Code".
    /// </summary>
    public virtual HtmlString Register_Calling_Code => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_CallingCode));

    /// <summary>
    ///   Gets the localized string for "Choose a username and a password of your choice. You can periodically change your password or whenever you wish to.".
    /// </summary>
    public virtual HtmlString Register_Choose_A_Username_And_A_Password => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_ChooseUsernameAndPassword_FieldLabel));

    /// <summary>
    ///   Gets the localized string for "Choose an email and a password of your choice. You can periodically change your password or whenever you wish to.".
    /// </summary>
    public virtual HtmlString Register_Choose_An_Email_And_A_Password => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_ChooseEmailAndPassword_FieldLabel));

    /// <summary>
    ///   Gets the localized string for "First name".
    /// </summary>
    public virtual HtmlString Register_First_Name => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_FirstName));

    /// <summary>
    ///   Gets the localized string for "here".
    /// </summary>
    public virtual HtmlString Register_Here => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_here));

    /// <summary>
    /// Text for "login with" label on Register page.
    /// </summary>
    public virtual HtmlString Register_login_with => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_LoginWith));

    /// <summary>
    ///   Gets the localized string for "I consent to the registration and processing of the above personal details for my contact and service as they are defined".
    /// </summary>
    /// <remarks>If you need to change link then prefer setting PrivacyUrl in IdentityUIOptions</remarks>
    public virtual HtmlString Register_I_Consent_To_Registration(string url = "/privacy") => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_ConsentToRegistrationAndProcessing, url));

    /// <summary>
    ///   Gets the localized string for "I consent to the use of my contact information, including my email address, for the purpose of receiving commercial communications, promotional materials, and marketing updates from the organization. I understand that I may withdraw my consent at any time".
    /// </summary>
    public virtual HtmlString Register_I_Consent_To_Contact_Usage => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_ConsentToContactUsage));

    /// <summary>
    ///   Gets the localized string for "Join us".
    /// </summary>
    public virtual HtmlString Register_Join_Us => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_JoinUs));

    /// <summary>
    ///   Gets the localized string for "Last name".
    /// </summary>
    public virtual HtmlString Register_Last_Name => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_LastName));

    /// <summary>
    ///   Gets the localized string for "OR".
    /// </summary>
    public virtual HtmlString Register_OR => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_OR));

    /// <summary>
    ///   Gets the localized string for "Password_FieldLabel".
    /// </summary>
    public virtual HtmlString Register_Password => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Password_FieldLabel));

    /// <summary>
    ///   Gets the localized string for "Phone number".
    /// </summary>
    public virtual HtmlString Register_Phone_number => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_PhoneNumber));

    /// <summary>
    ///   Gets the localized string for "Sign up".
    /// </summary>
    public virtual HtmlString Register_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_PageTitle));

    /// <summary>
    ///   Gets the localized string for "These credentials are personal. Please remember them and do not reveal in any way (i.e orally, written, email) in third parties".
    /// </summary>
    public virtual HtmlString Register_Credentials_Notice => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_KeepCredentialsPrivateHint));

    /// <summary>
    ///   Gets the localized string for "Timezone".
    /// </summary>
    public virtual HtmlString Register_Timezone => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Timezone));

    /// <summary>
    ///   Gets the localized string for "Username".
    /// </summary>
    public virtual HtmlString Register_Username => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Username));

    #endregion

    #region SetLanguage


    /// <summary>
    /// Status message shown while the UI language is being set.
    /// </summary>
    public virtual HtmlString SetLanguage_Setting_ui_language => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.SetLanguage_Setting_ui_language));

    #endregion

    #region VerifyPhone

    /// <summary>
    ///   Gets the localized string for "Code".
    /// </summary>
    public virtual HtmlString VerifyPhone_Code => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.VerifyPhone_Code));

    /// <summary>
    ///   Gets the localized string for "Next".
    /// </summary>
    public virtual HtmlString VerifyPhone_Next => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.VerifyPhone_Next));

    /// <summary>
    ///   Gets the localized string for "Resend".
    /// </summary>
    public virtual HtmlString VerifyPhone_Resend => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.VerifyPhone_Resend));

    /// <summary>
    ///   Gets the localized string for "Save".
    /// </summary>
    public virtual HtmlString VerifyPhone_Save => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.VerifyPhone_Save));

    /// <summary>
    ///   Gets the localized string for "Verify phone number".
    /// </summary>
    public virtual HtmlString VerifyPhone_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.VerifyPhone_PageTitle));

    /// <summary>
    /// Text for "OTP is valid till:" label on the Verify Phone page.
    /// </summary>
    public virtual HtmlString VerifyPhone_OTP_is_valid_till => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.VerifyPhone_OTP_is_valid_till));

    #endregion

    /// <summary> Label for the Preferred Language field in profile sidebar.</summary>     
    public virtual HtmlString ProfileSidebar_ConfirmEmailSentTo => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_ConfirmEmailSentTo));
    /// <summary> Label for Manage Profile action in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_NewEmailConfirmationSent => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_NewEmailConfirmationSent));
    /// <summary> Placeholder text for dropdowns or selection inputs in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_BirthDate => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_BirthDate));
    /// <summary> Email label in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_CallingCode => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_CallingCode));
    /// <summary> Label for timezone selection in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_Confirmation => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_Confirmation));
    /// <summary> Gets the localized string for "Confirmation email delivery failed. Please contact system administrator." in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_ConfirmationEmailFailed => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_ConfirmationEmailFailed));
    /// <summary> Gets the localized string for "Connect a new provider" in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_ConnectNewProvider => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_ConnectNewProvider));
    /// <summary> Gets the localized string for "Developer TOTP" in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_DeveloperTOTP => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_DeveloperTOTP));
    /// <summary> Gets the localized string for "Existing providers" in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_ExistingProviders => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_ExistingProviders));
    /// <summary> Gets the localized string for "External providers" in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_ExternalProviders => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_ExternalProviders));
    /// <summary> Gets the localized string for "First name" in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_FirstName => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_FirstName));
    /// <summary> Gets the localized string for "here" in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_Here => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_Here));
    /// <summary> Gets the localized string for "I have been informed about the processing of my personal data and I consent to it, as specifically defined" in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_DataProcessingConsent => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_DataProcessingConsent));
    /// <summary> Gets the localized string for "Language" in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_LastName => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_LastName));
    /// <summary> Gets the localized string for "Last name" in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_PhoneNumber => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_PhoneNumber));
    /// <summary> Gets the localized string for "Preferences" in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_Profile => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_Profile));
    /// <summary> Gets the localized string for "Profile" in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_Remove => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_Remove));
    /// <summary> Gets the localized string for "Save" in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_Save => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_Save));
    /// <summary> Gets the localized string for "Tax identification" in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_TaxIdentification => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_TaxIdentification));
    /// <summary> Gets the localized string for "Unknown" in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_Unknown => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_Unknown));
    /// <summary> Gets the localized string for "Username" in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_Username => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_Username));
    /// <summary> Gets the localized string for "Your new email verification is still pending." in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_NewEmailVerificationPending => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_NewEmailVerificationPending));
    /// <summary> Gets the localized string for "Your profile was updated successfully." in profile sidebar.</summary>
    public virtual HtmlString ProfileSidebar_ProfileUpdatedSuccessfully => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ProfileSidebar_ProfileUpdatedSuccessfully));

    /// <summary>Title of the Privacy policy page.</summary>
    public virtual HtmlString Privacy_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Privacy_PageTitle));

    /// <summary>Header of the Privacy policy page.</summary>
    public virtual HtmlString Privacy_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Privacy_PageHeader));

    /// <summary>Required label</summary>
    public virtual HtmlString Required => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.RequiredValue));

    /// <summary>Title of the Terms of Service page.</summary>
    public virtual HtmlString Terms_PageTitle => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Terms_PageTitle));


    /// <summary>Header of the Terms of Service page.</summary>
    public virtual HtmlString Terms_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Terms_PageHeader));


    /// <summary>Get Organization name.</summary>
    public virtual HtmlString OrganizationName(string organization) {
        var orgName = string.Format(CultureInfo.CurrentUICulture, IdentityLabels.OrganizationName);
        if (string.IsNullOrWhiteSpace(orgName)) return new HtmlString(organization);
        return new HtmlString(orgName);
    }
    /// <summary>Get Organization name.</summary>
    public virtual string ApplicationName(string applicationName) {
        return applicationName;
    }
    /// <summary>Get Organization name.</summary>
    public virtual HtmlString GetGeneric(string label) => new HtmlString(string.Format(CultureInfo.CurrentUICulture, label));

    #region PageHeaders
    // Added unified PageHeader properties for use in <vc:page-heading />.

    /// <summary>Gets the HTML-formatted page header displayed to users who are changing their password. </summary>
    public virtual HtmlString AddPassword_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPassword_PageHeader));
    /// <summary>Gets the HTML-formatted page header displayed to users who are adding a phone number to their account. </summary>
    public virtual HtmlString AddPhone_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPhone_PageHeader));
    /// <summary>Gets the HTML-formatted page header displayed to users who are associating an external login provider with their account. </summary>
    public virtual HtmlString Associate_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_PageHeader));
    /// <summary>Gets the HTML-formatted page header displayed to users who are confirming their email address. </summary>
    public virtual HtmlString ConfirmEmail_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_PageHeader));
    /// <summary>Gets the HTML-formatted page header displayed to users who are confirming an email change. </summary>
    public virtual HtmlString ConfirmEmailChange_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_PageHeader));
    /// <summary>Gets the HTML-formatted page header displayed to users who have requested a password reset. </summary>
    public virtual HtmlString ForgotPassword_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPassword_PageHeader));
    /// <summary>Gets the HTML-formatted page header displayed to users after they have requested a password reset. </summary>
    public virtual HtmlString ForgotPasswordConfirmation_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPasswordConfirmation_PageHeader));
    /// <summary>Gets the HTML-formatted page header displayed to users who have logged out. </summary>
    public virtual HtmlString LoggedOut_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.LoggedOut_PageHeader));
    /// <summary>Gets the localized HTML content for the page header displayed during login. </summary>
    public virtual HtmlString Login_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_PageHeader));
    /// <summary>Gets the localized HTML content for the page header displayed during logout. </summary>
    public virtual HtmlString Logout_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Logout_PageHeader));
    /// <summary>Gets the localized HTML content for the page header displayed during MFA authentication. </summary>
    public virtual HtmlString Mfa_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_PageHeader));
    /// <summary>Gets the localized HTML content for the page header displayed during MFA onboarding. </summary>
    public virtual HtmlString MfaOnBoarding_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoarding_PageHeader));
    /// <summary>Gets the localized HTML content for the page header displayed during MFA onboarding phone addition. </summary>
    public virtual HtmlString MfaOnBoardingAddPhone_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingAddPhone_PageHeader));
    /// <summary>Gets the localized HTML content for the page header displayed during MFA onboarding phone verification. </summary>
    public virtual HtmlString MfaOnBoardingVerifyPhone_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingVerifyPhone_PageHeader));
    /// <summary>Gets the localized HTML content for the page header displayed on the password expired screen.</summary>
    public virtual HtmlString PasswordExpired_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.PasswordExpired_PageHeader));
    /// <summary>Gets the localized HTML content for the page header displayed on the registration screen.</summary>
    public virtual HtmlString Register_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_PageHeader));
    /// <summary>Gets the localized HTML content for the page header displayed during phone number verification.</summary>
    public virtual HtmlString VerifyPhone_PageHeader => new HtmlString(string.Format(CultureInfo.CurrentUICulture, IdentityLabels.VerifyPhone_PageHeader));
    #endregion
}