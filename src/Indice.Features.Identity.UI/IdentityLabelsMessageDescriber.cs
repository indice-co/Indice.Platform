using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Identity.UI;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using static System.Collections.Specialized.BitVector32;


namespace Indice.Features.Identity.UI;
/// <summary>
/// Provides descriptive labels and messages for identity-related UI elements.
/// </summary>
public class IdentityLabelsMessageDescriber
{
    #region AcceptTerms
    /// <summary>Text for the Accept button on the Accept Terms page.</summary>
    public virtual string AcceptTerms_Accept => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AcceptTerms_Accept);

    /// <summary>Text for the Reject button on the Accept Terms page.</summary>
    public virtual string AcceptTerms_Reject => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AcceptTerms_Reject);

    /// <summary>Title of the Accept Terms page.</summary>
    public virtual string AcceptTerms_Terms___conditions_acceptance => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AcceptTerms_Terms___conditions_acceptance);

    /// <summary>Instruction message prompting the user to read and accept the terms and conditions.</summary>
    public virtual string AcceptTerms_Please_read_and_accept_the_terms_and_conditions_to_continue => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AcceptTerms_Please_read_and_accept_the_terms_and_conditions_to_continue_);
    #endregion

    #region AddEmail
    /// <summary>Label for the Next button on the Add Email page, formatted with the provided email.</summary>
    public virtual string AddEmail_Next(string email) => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddEmail_Next, email);

    /// <summary>Label for the Save button on the Add Email page.</summary>
    public virtual string AddEmail_Save => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddEmail_Save);

    /// <summary>Instruction message prompting the user to verify their email.</summary>
    public virtual string AddEmail_Verify_email => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddEmail_Verify_email);
    #endregion

    #region AddPassword
    /// <summary>Label for the Add Password button.</summary>
    public virtual string AddPassword_Add => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPassword_Add);

    /// <summary>Label for the New Password input field.</summary>
    public virtual string AddPassword_Add_Password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPassword_Add_Password);

    /// <summary>Label for the New Password input field (alternate reference).</summary>
    public virtual string AddPassword_New_password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPassword_New_password);

    /// <summary>Label for the Confirm Password input field.</summary>
    public virtual string AddPassword_Confirm_password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPassword_Password_confirmation);

    /// <summary>Success message displayed when the password has been successfully added.</summary>
    public virtual string AddPassword_Password_successfully_added => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPassword_Password_successfully_added);

    /// <summary>Message indicating that the password addition process has been completed.</summary>
    public virtual string AddPassword_Process_completed => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPassword_Process_completed);
    #endregion


    #region AddPhone
    /// <summary>Label for the Add Phone action button.</summary>
    public virtual string AddPhone_Add => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPhone_Add_phone);
    /// <summary>Title of the Add Phone page.</summary>
    public virtual string AddPhone_Add_phone_number => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPhone_Add_phone_number);
    /// <summary>Instruction message indicating the calling code selection for the phone number.</summary>
    public virtual string AddPhone_Calling_Code => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPhone_Calling_Code);
    /// <summary>Label for the Phone Number input field.</summary>
    public virtual string AddPhone_Phone_number => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPhone_Phone_number);
    /// <summary>Label for the Save button on the Add Phone page.</summary>
    public virtual string AddPhone_Save => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.AddPhone_Save);
    #endregion

    #region Challenge

    /// <summary>
    /// Message shown when redirecting during an authentication challenge.
    /// </summary>
    public virtual string Challenge_Redirecting => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Challenge_Redirecting);

    #endregion

    #region ChangePassword
    /// <summary>Label for the Change Password action button.</summary>
    public virtual string ChangePassword_Change => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ChangePassword_Change);
    /// <summary>Label for the Current Password input field.</summary>
    public virtual string ChangePassword_Change_password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ChangePassword_Change_Password);
    /// <summary>Label for the New Password input field.</summary>
    public virtual string ChangePassword_New_password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ChangePassword_New_password);
    /// <summary>Label for the Old Password input field.</summary>
    public virtual string ChangePassword_Old_password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ChangePassword_Old_password);
    /// <summary>Success message displayed when the password has been successfully changed.</summary>
    public virtual string ChangePassword_Password_Successfully_Changed => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ChangePassword_Password_successfully_changed);
    /// <summary>Message displayed when the password change process is completed.</summary>
    public virtual string ChangePassword_Process_Completed => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ChangePassword_Process_completed_);
    #endregion


    #region ConfirmEmail
    /// <summary>Text for the Confirm Email button.</summary>
    public virtual string ConfirmEmail_Click => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_Click);
    /// <summary>Title of the Confirm Email page.</summary>
    public virtual string ConfirmEmail_Email_Confirmation => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_Email_Confirmation);
    /// <summary>Error message displayed when the email verification link has expired.</summary>
    public virtual string ConfirmEmail_Email_Verification_Link_Has_Expired => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_Email_verification_link_has_expired_);
    /// <summary>Instruction message guiding the user on the Confirm Email page.</summary>
    public virtual string ConfirmEmail_Here => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_here);
    /// <summary>Instruction displayed if the user received the error by mistake.</summary>
    public virtual string ConfirmEmail_If_You_Feel_You_Got_This_Error_By_Mistake_Simply_Click => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_If_you_feel_you_got_this_error_by_mistake__simply_click);
    /// <summary>Error message displayed when no action was taken.</summary>
    public virtual string ConfirmEmail_No_Action_Was_Taken => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_No_action_was_taken_);
    /// <summary>Instruction displayed when the user needs to log in and resend the verification link.</summary>
    public virtual string ConfirmEmail_Please_Log_In_And_Resend_The_Link => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_Please_log_in_and_resend_the_link);
    /// <summary>Instruction message indicating that verification may take a moment and the user should click the link below.</summary>
    public virtual string ConfirmEmail_This_May_Take_A_Moment_Click_On_The_Link_Below_To_Verify_Your_Email_Address => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_This_may_take_a_moment__Click_on_the_link_below_to_verify_your_email_address_);
    /// <summary>Text for navigating back to sign in or return to the previous page.</summary>
    public virtual string ConfirmEmail_To_Sign_In_Or_Go_Back => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_to_sign_in_or_go_back_);
    /// <summary>Label for the Verify action button.</summary>
    public virtual string ConfirmEmail_Verify => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_Verify);
    /// <summary>Instruction displayed while verifying the user's email.</summary>
    public virtual string ConfirmEmail_Verifying_Your_Email => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_Verifying_your_email_);
    /// <summary>Instruction indicating the user can close the browser window after verification.</summary>
    public virtual string ConfirmEmail_You_Can_Now_Close_This_Browse_Window => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_You_can_now_close_this_browse_window_);
    /// <summary>Success message displayed when the email has been successfully confirmed.</summary>  
    public virtual string ConfirmEmail_Your_Email_Has_Been_Successfully_Confirmed => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmail_Your_email_has_been_successfully_confirmed_);
    #endregion

    #region ConfirmEmailChange
    /// <summary>Text for the Confirm Email Change button.</summary>
    public virtual string ConfirmEmailChange_Click => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_Click);
    /// <summary>Title of the Confirm Email Change page.</summary>
    public virtual string ConfirmEmailChange_Email_Change_Confirmation => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_Email_Change_Confirmation);
    /// <summary>Error message displayed when the email verification link has expired.</summary>
    public virtual string ConfirmEmailChange_Email_Verification_Link_Has_Expired => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_Email_verification_link_has_expired_);
    /// <summary>Instruction message guiding the user on the Confirm Email Change page.</summary>
    public virtual string ConfirmEmailChange_Here => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_here);
    /// <summary>Instruction displayed if the user received the error by mistake.</summary>
    public virtual string ConfirmEmailChange_If_You_Feel_You_Got_This_Error_By_Mistake_Simply_Click => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_If_you_feel_you_got_this_error_by_mistake__simply_click);
    /// <summary>Error message displayed when no action was taken.</summary>
    public virtual string ConfirmEmailChange_No_Action_Was_Taken => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_No_action_was_taken_);
    /// <summary>Instruction displayed when the user needs to log in and resend the verification link.</summary>
    public virtual string ConfirmEmailChange_Please_Log_In_And_Resend_The_Link => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_Please_log_in_and_resend_the_link);
    /// <summary>Instruction message indicating that verification may take a moment and the user should click the link below.</summary>
    public virtual string ConfirmEmailChange_This_May_Take_A_Moment_Click_On_The_Link_Below_To_Verify_Your_Email_Address => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_This_may_take_a_moment__Click_on_the_link_below_to_verify_your_email_address_);
    /// <summary>Text for navigating back to sign in or return to the previous page.</summary>
    public virtual string ConfirmEmailChange_To_Sign_In_Or_Go_Back => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_to_sign_in_or_go_back_);
    /// <summary>Label for the Verify action button.</summary>
    public virtual string ConfirmEmailChange_Verify => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_Verify);
    /// <summary>Instruction displayed while verifying the user's email.</summary>
    public virtual string ConfirmEmailChange_Verifying_Your_Email => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_Verifying_your_email_);
    /// <summary>Instruction indicating the user can close the browser window after verification.</summary>
    public virtual string ConfirmEmailChange_You_Can_Now_Close_This_Browse_Window => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_You_can_now_close_this_browse_window_);
    /// <summary>Success message displayed when the email has been successfully changed.</summary>
    public virtual string ConfirmEmailChange_Your_Email_Has_Been_Successfully_Changed => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_Your_email_has_been_successfully_changed_);
    /// <summary>Message displayed when the email is already verified.</summary>
    public virtual string ConfirmEmailChange_Your_Email_Is_Already_Verified_Thank_You => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ConfirmEmailChange_Your_email_is_already_verified__Thank_you);
    #endregion


    #region Consent
    /// <summary>Gets the label text for the Accept button on the Consent page.</summary>
    public virtual string Consent_Accept => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_Accept);
    /// <summary>Gets the label text displaying the application name.</summary>
    public virtual string Consent_App => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_app);
    /// <summary>Gets the label for the Application Access section.</summary>
    public virtual string Consent_Application_Access => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_Application_Access);
    /// <summary>Gets the label text for the Cancel action.</summary>
    public virtual string Consent_Cancel => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_Cancel);
    /// <summary>Gets the label text for the Code input field.</summary>
    public virtual string Consent_Code => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_code);
    /// <summary>Gets the label text for the Consent action.</summary>
    public virtual string Consent_Consent => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_Consent);
    /// <summary>Gets the label text for personal information section.</summary>
    public virtual string Consent_Personal_Information => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_Personal_Information);
    /// <summary>Gets the label text for the Remember My Decision checkbox.</summary>
    public virtual string Consent_Remember_My_Decision => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_Remember_My_Decision);
    /// <summary>Gets the label text for the Security Code input field.</summary>
    public virtual string Consent_Security_Code => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_Security_code);
    /// <summary>Gets the label text for the Send action.</summary>
    public virtual string Consent_Send => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_send);
    /// <summary>Gets the label text for the Permissions section title.</summary>
    public virtual string Consent_This => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_This);
    /// <summary>Gets the text indicating what the current app would like to access.</summary>
    public virtual string Consent_This_App_Would_Like_To => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_This_app_would_like_to);
    /// <summary>Gets the text describing access to the user's phone.</summary>
    public virtual string Consent_To_Your_Phone => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_to_your_phone);
    /// <summary>Gets the instruction to uncheck permissions the user does not wish to grant.</summary>
    public virtual string Consent_Uncheck_The_Permissions_You_Do_Not_Wish_To_Grant => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_Uncheck_the_permissions_you_do_not_wish_to_grant_);
    /// <summary>Gets the text indicating the app’s request for permission.</summary>
    public virtual string Consent_Would_Like_To => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Consent_would_like_to);
    #endregion


    #region Error

    /// <summary>
    /// Label for the "Go back home" action, typically shown on error pages.
    /// </summary>
    public virtual string Error_Go_back_home => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Error_Go_back_home);

    /// <summary>Gets the title text for the Error page.</summary>
    public virtual string Error_Oops_Seems_We_Encountered_An_Error => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Error_Oops__Seems_we_encountered_an_error_);
    /// <summary>Gets the label text displaying the request ID for error tracking.</summary>
    public virtual string Error_Request_Id => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Error_Request_Id);
    #endregion

    #region ForgotPassword
    /// <summary>Gets the title text for the Forgot Password page.</summary>
    public virtual string ForgotPassword_Forgot_Password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPassword_Forgot_password);
    /// <summary>Gets the message indicating that a password reset request has been sent successfully.</summary>
    public virtual string ForgotPassword_Request_Sent => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPassword_Request_sent);
    /// <summary>Gets the label text for resending the password reset email.</summary>
    public virtual string ForgotPassword_Resend => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPassword_Resend);
    /// <summary>Gets the label text for the Send button on the Forgot Password page.</summary>
    public virtual string ForgotPassword_Send => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPassword_Send);
    /// <summary>Gets the instructional text guiding the user to enter their username or email to receive a password reset link.</summary>
    public virtual string ForgotPassword_To_Have_Your_Password_Reset_Enter_Your_Username_Or_Email_Address_Below_We_Will_Then_Send_An_Email_Containing_A_Link_To_Reset_Your_Password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPassword_To_have_your_password_reset__enter_your_username_or_email_address_below__We_will_then_send_an_email_containing_a_link_to_reset_your_password_);
    /// <summary>Gets the label text for the Email or Username input field.</summary>
    public virtual string ForgotPassword_Username => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPassword_Username);
    #endregion

    #region ForgotPasswordConfirmation
    /// <summary>Gets the title text for the Forgot Password Confirmation page.</summary>
    public virtual string ForgotPasswordConfirmation_Forgot_Password_Confirmation => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPasswordConfirmation_Forgot_Password_Confirmation);
    /// <summary>Gets the instruction text prompting the user to enter a new password.</summary>
    public virtual string ForgotPasswordConfirmation_New_Password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPasswordConfirmation_New_Password);
    /// <summary>Gets the message indicating that the password has been successfully changed.</summary>
    public virtual string ForgotPasswordConfirmation_Password_Changed => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPasswordConfirmation_Password_changed);
    /// <summary>Gets the instruction text asking the user to log in using their new password. Includes a link for login.</summary>
    /// <param name="callbackUrl">The URL to the login page.</param>
    public virtual string ForgotPasswordConfirmation_Please_Login_With_Your_New_Password(string callbackUrl) => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPasswordConfirmation_Please_fill_in_your_new_password, callbackUrl);
    /// <summary>Gets the label prompting the user to fill in their new password.</summary>
    public virtual string ForgotPasswordConfirmation_Please_Fill_In_Your_New_Password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPasswordConfirmation_Please_fill_in_your_new_password);
    /// <summary>Gets the text for the "Send" button on the Forgot Password Confirmation page.</summary>
    public virtual string ForgotPasswordConfirmation_Send => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.ForgotPasswordConfirmation_Send);
    #endregion

    #region Grants
    /// <summary>Gets the header text for the Grants page listing authorized applications and resources.</summary>
    public virtual string Grants_Applications_And_Resources => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Grants_Below_is_the_list_of_applications_you_have_given_access_to_and_the_names_of_the_resources_they_have_access_to_);
    /// <summary>Gets the label text for the client logo.</summary>
    public virtual string Grants_Client_Logo => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Grants_Client_logo);
    /// <summary>Gets the label text indicating when a grant was created.</summary>
    public virtual string Grants_Created => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Grants_Created_);
    /// <summary>Gets the label text indicating when a grant expires.</summary>
    public virtual string Grants_Expires_On => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Grants_Expires_on);
    /// <summary>Gets the label text for the grants section.</summary>
    public virtual string Grants_Grants => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Grants_Grants);
    /// <summary>Gets the text for the action to revoke access from an application or resource.</summary>
    public virtual string Grants_Revoke_Access => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Grants_Revoke_Access);
    /// <summary>Gets the message displayed when no applications have been granted access.</summary>
    public virtual string Grants_No_Applications_Provided => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Grants_You_have_not_provided_access_to_any_applications);
    #endregion


    #region Home
    /// <summary>Title of the Home page.</summary>
    public virtual string Home_Welcome_Portal_Of(string portalName) => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home__strong_Welcome__strong__br__to_our_Digital_Services_Portal_of__0_, portalName);
    /// <summary>Instruction message on the Home page.</summary>
    public virtual string Home_Authorized_Applications => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_authorized_applications);
    /// <summary>Instruction message on the Home page.</summary>
    public virtual string Home_Check_And_Revoke_Your => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_Check_and_revoke_your);
    /// <summary>Instruction message on the Home page.</summary>
    public virtual string Home_Digital_Services => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_Digital_Services);
    /// <summary>Instruction message on the Home page.</summary>
    public virtual string Home_Here => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_here);
    /// <summary>Instruction message on the Home page.</summary>
    public virtual string Home_Manage_Your_Grants => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_Manage_your_grants);
    /// <summary>Instruction message on the Home page.</summary>
    public virtual string Home_Please_Login_To_The_Application => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_Please_login_to_the_application);
    /// <summary>Instruction message on the Home page.</summary>
    public virtual string Home_Portal => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_Portal);
    /// <summary>Instruction message on the Home page.</summary>
    public virtual string Home_Identity_Portal_Info(string serviceName) => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_The_Identity_portal_gives_you_access_to_all__0__services_with_one_account__A_place_to_manage_your_user_profile_and_account_access_settings__Use_the_links_below_to_get_started_, serviceName);
    /// <summary>Instruction message on the Home page.</summary>
    public virtual string Home_Welcome => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_Welcome);
    /// <summary>Instruction message on the Home page.</summary>
    public virtual string Home_Welcome_Back => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_Welcome_back);
    /// <summary>Instruction message on the Home page.</summary>
    public virtual string Home_Welcome_To_The_Digital_Services_Portal(string serviceName) => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Home_Welcome_to_the__0__Digital_Services__strong_Portal__strong_, serviceName);
    #endregion

    #region LoggedOut
    /// <summary>Title of the Logged Out page.</summary>
    public virtual string LoggedOut_Click => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.LoggedOut_Click);
    /// <summary>Instruction message on the Logged Out page.</summary>
    public virtual string LoggedOut_Here => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.LoggedOut_here);
    /// <summary>Instruction message on the Logged Out page.</summary>
    public virtual string LoggedOut_Logout => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.LoggedOut_Logout);
    /// <summary>Instruction message on the Logged Out page.</summary>
    public virtual string LoggedOut_To_Return_To_The_Application => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.LoggedOut_to_return_to_the_application);
    /// <summary>Success message when the user has been logged out.</summary>
    public virtual string LoggedOut_You_Are_Now_Logged_Out => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.LoggedOut_You_are_now_logged_out);
    #endregion

    #region Login

    /// <summary>
    ///   Gets the localized string for "Don't have an account?" on the Login page.
    /// </summary>
    public virtual string Login_Dont_Have_An_Account => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_Don_t_have_an_account_);

    /// <summary>
    ///   Gets the localized string for "Forgot Password" instruction on the Login page.
    /// </summary>
    public virtual string Login_Forgot_Password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_Forgot_password_);

    /// <summary>
    ///   Gets the localized string for "Invalid login request" instruction on the Login page.
    /// </summary>
    public virtual string Login_Invalid_Login_Request => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_Invalid_login_request);

    /// <summary>
    ///   Gets the localized string for "Join Us" instruction on the Login page.
    /// </summary>
    public virtual string Login_Join_Us => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_Join_us);

    /// <summary>
    ///   Gets the localized string for "Login" label.
    /// </summary>
    public virtual string Login_Login =>
        string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_Login);

    /// <summary>
    ///   Gets the localized string for "OR" label.
    /// </summary>
    public virtual string Login_OR => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_OR);

    /// <summary>
    ///   Gets the localized string for "Password" label.
    /// </summary>
    public virtual string Login_Password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_Password);

    /// <summary>
    ///   Gets the localized string for "Remember me" option.
    /// </summary>
    public virtual string Login_Remember_Me => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_Remember_me);

    /// <summary>
    ///   Gets the localized string for "Sign In" button.
    /// </summary>
    public virtual string Login_Sign_In => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_Sign_in);

    /// <summary>
    ///   Gets the localized string for "No login schemes configured" message.
    /// </summary>
    public virtual string Login_No_Login_Schemes_Configured => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_There_are_no_login_schemes_configured_for_this_client_);

    /// <summary>
    ///   Gets the localized string for "Username" label.
    /// </summary>
    public virtual string Login_Username => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Login_Username);

    #endregion

    #region Logout

    /// <summary>
    ///   Gets the localized string for "Logout" button.
    /// </summary>
    public virtual string Logout_Logout => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Logout_Logout);

    /// <summary>
    ///   Gets the localized string for the confirmation message "Would you like to logout?".
    /// </summary>
    public virtual string Logout_Would_You_Like_To_Logout => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Logout_Would_you_like_to_logout_);

    /// <summary>
    ///   Gets the localized string for "Yes" button on the logout confirmation dialog.
    /// </summary>
    public virtual string Logout_Yes => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Logout_Yes);

    #endregion



    #region Mfa

    /// <summary>
    ///   Gets the localized string for "An unexpected error occurred while sending the OTP code".
    /// </summary>
    public virtual string Mfa_An_Unexpected_Error_Occurred_While_Sending_The_OTP_Code => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_An_unexpected_error_occurred_while_sending_the_OTP_code_);

    /// <summary>
    ///   Gets the localized string for "Authenticate".
    /// </summary>
    public virtual string Mfa_Authenticate => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_Authenticate);

    /// <summary>
    ///   Gets the localized string for "Authenticate yourself using...".
    /// </summary>
    public virtual string Mfa_Authenticate_Yourself_Using => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_Authenticate_yourself_using_);

    /// <summary>
    ///   Gets the localized string for "Back".
    /// </summary>
    public virtual string Mfa_Back => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_Back);

    /// <summary>
    ///   Gets the localized string for "Because you've turned on two-step verification, you need to approve request on your mobile app".
    /// </summary>
    public virtual string Mfa_Because_Youve_Turned_On_Two_Step_Verification_You_Need_To_Approve_Request_On_Your_Mobile_App => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_Because_you_ve_turned_on_two_step_verification__you_need_to_approve_request_on_your_mobile_app_);

    /// <summary>
    ///   Gets the localized string for "I can't use my app right now".
    /// </summary>
    public virtual string Mfa_I_Cant_Use_My_App_Right_Now => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_I_can_t_use_my_app_right_now);

    /// <summary>
    ///   Gets the localized string for "I didn't receive the notification. Resend".
    /// </summary>
    public virtual string Mfa_I_Didnt_Receive_The_Notification_Resend => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_I_didn_t_receive_the_notification_Resend_);

    /// <summary>
    ///   Gets the localized string for "I sign in frequently here. Remember this browser".
    /// </summary>
    public virtual string Mfa_I_Sign_In_Frequently_Here_Remember_This_Browser => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_I_sign_in_frequently_here__Remember_this_browser_);

    /// <summary>
    ///   Gets the localized string for "Login".
    /// </summary>
    public virtual string Mfa_Login => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_Login);

    /// <summary>
    ///   Gets the localized string for "Multifactor Authentication".
    /// </summary>
    public virtual string Mfa_Multifactor_Authentication => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_Multifactor_Authentication);

    /// <summary>
    ///   Gets the localized string for "Other authentication methods...".
    /// </summary>
    public virtual string Mfa_Other_Authentication_Methods => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_Other_authentication_methods___);

    /// <summary>
    ///   Gets the localized string for "OTP Code".
    /// </summary>
    public virtual string Mfa_OTP_Code => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_OTP_Code);

    /// <summary>
    ///   Gets the localized string for "Request denied".
    /// </summary>
    public virtual string Mfa_Request_Denied => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_Request_denied);

    /// <summary>
    ///   Gets the localized string for "Resend".
    /// </summary>
    public virtual string Mfa_Resend => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_Resend);

    /// <summary>
    ///   Gets the localized string for "Send another request to my app".
    /// </summary>
    public virtual string Mfa_Send_Another_Request_To_My_App => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_Send_another_request_to_my_app);

    /// <summary>
    ///   Gets the localized string for "We sent an identity verification request to your mobile device, but you denied it".
    /// </summary>
    public virtual string Mfa_We_Sent_An_Identity_Verification_Request_To_Your_Mobile_Device_But_You_Denied_It => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_We_sent_an_identity_verification_request_to_your_mobile_device__but_you_denied_it_);

    /// <summary>
    ///   Gets the localized string for "We texted your phone {0}. Please enter the code to sign in" with a phone number parameter.
    /// </summary>
    public virtual string Mfa_We_Texted_Your_Phone(string phoneNumber) => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Mfa_We_texted_your_phone__0___Please_enter_the_code_to_sign_in_, phoneNumber);

    #endregion


    #region MfaModel

    /// <summary>
    ///   Gets the localized string for "OTP login".
    /// </summary>
    public virtual string MfaModel_OTP_Login => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaModel_OTP_login);

    /// <summary>
    ///   Gets the localized string for "The OTP code is not valid."
    /// </summary>
    public virtual string MfaModel_The_OTP_Code_Is_Not_Valid => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaModel_The_OTP_code_is_not_valid_);

    /// <summary>
    ///   Gets the localized string for "Your OTP code for login is: {0}" with a code parameter.
    /// </summary>
    public virtual string MfaModel_Your_OTP_Code_For_Login(string code) => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaModel_Your_OTP_code_for_login_is___0_, code);

    #endregion

    #region MfaOnBoarding

    /// <summary>
    ///   Gets the localized string for "Enable MFA".
    /// </summary>
    public virtual string MfaOnBoarding_Enable_MFA => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoarding_Enable_MFA);

    /// <summary>
    ///   Gets the localized string for "Keep your account safe".
    /// </summary>
    public virtual string MfaOnBoarding_Keep_Your_Account_Safe => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoarding_Keep_your_account_safe);

    /// <summary>
    ///   Gets the localized string for "Next".
    /// </summary>
    public virtual string MfaOnBoarding_Next => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoarding_Next);

    /// <summary>
    ///   Gets the localized string for "Setup an additional authentication method".
    /// </summary>
    public virtual string MfaOnBoarding_Setup_An_Additional_Authentication_Method => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoarding_Setup_an_additional_authentication_method_);

    #endregion

    #region MfaOnBoardingAddPhone

    /// <summary>
    ///   Gets the localized string for "MFA onboarding - SMS".
    /// </summary>
    public virtual string MfaOnBoardingAddPhone_MFA_Onboarding_SMS => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingAddPhone_MFA_onboarding___SMS);

    /// <summary>
    ///   Gets the localized string for "Next".
    /// </summary>
    public virtual string MfaOnBoardingAddPhone_Next => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingAddPhone_Next);

    /// <summary>
    ///   Gets the localized string for "Phone number".
    /// </summary>
    public virtual string MfaOnBoardingAddPhone_Phone_Number => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingAddPhone_Phone_number);

    /// <summary>
    ///   Gets the localized string for "Save".
    /// </summary>
    public virtual string MfaOnBoardingAddPhone_Save => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingAddPhone_Save);

    #endregion



    #region MfaOnBoardingVerifyPhone

    /// <summary>
    ///   Gets the localized string for "Code".
    /// </summary>
    public virtual string MfaOnBoardingVerifyPhone_Code => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingVerifyPhone_Code);

    /// <summary>
    ///   Gets the localized string for "Next".
    /// </summary>
    public virtual string MfaOnBoardingVerifyPhone_Next => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingVerifyPhone_Next);

    /// <summary>
    ///   Gets the localized string for "Save".
    /// </summary>
    public virtual string MfaOnBoardingVerifyPhone_Save => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingVerifyPhone_Save);

    /// <summary>
    ///   Gets the localized string for "Verify phone number".
    /// </summary>
    public virtual string MfaOnBoardingVerifyPhone_Verify_Phone_Number => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.MfaOnBoardingVerifyPhone_Verify_phone_number);

    #endregion

    #region PasswordExpired

    /// <summary>
    ///   Gets the localized string for "Change password".
    /// </summary>
    public virtual string PasswordExpired_Change_Password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.PasswordExpired_Change_password);

    /// <summary>
    ///   Gets the localized string for "New password".
    /// </summary>
    public virtual string PasswordExpired_New_Password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.PasswordExpired_New_password);

    /// <summary>
    ///   Gets the localized string for "Next".
    /// </summary>
    public virtual string PasswordExpired_Next => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.PasswordExpired_Next);

    /// <summary>
    ///   Gets the localized string for "Save".
    /// </summary>
    public virtual string PasswordExpired_Save => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.PasswordExpired_Save);

    /// <summary>
    ///   Gets the localized string for "New password confirmation".
    /// </summary>
    public virtual string PasswordExpired_New_Password_Confirmation => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.PasswordExpired_Νew_password_confirmation);

    #endregion


    #region Profile

    /// <summary>
    ///   Gets the localized string for "A confirmation email has been sent to {0}.".
    /// </summary>
    public virtual string Profile_A_Confirmation_Email_Has_Been_Sent_To_0(string email) => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_A_confirmation_email_has_been_sent_to__0__, email);

    /// <summary>
    /// Label for Manage Profile action.
    /// </summary>
    public virtual string Profile_Manage_Profile =>  string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Manage_Profile);

    /// <summary>
    /// Placeholder text for dropdowns or selection inputs in profile forms.
    /// </summary>
    public virtual string Profile_Choose => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Choose);

    /// <summary>
    /// Email label in profile forms.
    /// </summary>
    public virtual string Profile_Email =>string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Email);

    /// <summary>
    /// Label for timezone selection in profile forms.
    /// </summary>
    public virtual string Profile_Timezone => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Timezone);

    /// <summary>
    ///   Gets the localized string for "An email has been sent to your new email address in order to confirm it.".
    /// </summary>
    public virtual string Profile_An_Email_Has_Been_Sent_To_Your_New_Email_Address_In_Order_To_Confirm_It => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_An_email_has_been_sent_to_your_new_email_address_in_order_to_confirm_it_);

    /// <summary>
    ///   Gets the localized string for "Birth date".
    /// </summary>
    public virtual string Profile_Birth_Date => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Birth_date);

    /// <summary>
    ///   Gets the localized string for "Calling Code".
    /// </summary>
    public virtual string Profile_Calling_Code => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Calling_Code);

    /// <summary>
    ///   Gets the localized string for "Confirmation".
    /// </summary>
    public virtual string Profile_Confirmation => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Confirmation);

    /// <summary>
    ///   Gets the localized string for "Confirmation email delivery failed. Please contact system administrator.".
    /// </summary>
    public virtual string Profile_Confirmation_Email_Delivery_Failed_Please_Contact_System_Administrator => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Confirmation_email_delivery_failed__Please_contact_system_administrator_);

    /// <summary>
    ///   Gets the localized string for "Connect a new provider".
    /// </summary>
    public virtual string Profile_Connect_A_New_Provider => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Connect_a_new_provider);

    /// <summary>
    ///   Gets the localized string for "Developer TOTP".
    /// </summary>
    public virtual string Profile_Developer_TOTP => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Developer_TOTP);

    /// <summary>
    ///   Gets the localized string for "Existing providers".
    /// </summary>
    public virtual string Profile_Existing_Providers => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Existing_providers);

    /// <summary>
    ///   Gets the localized string for "External providers".
    /// </summary>
    public virtual string Profile_External_Providers => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_External_providers);

    /// <summary>
    ///   Gets the localized string for "First name".
    /// </summary>
    public virtual string Profile_First_Name => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_First_name);

    /// <summary>
    ///   Gets the localized string for "here".
    /// </summary>
    public virtual string Profile_Here => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_here);

    /// <summary>
    ///   Gets the localized string for "I have been informed about the processing of my personal data and I consent to it, as specifically defined".
    /// </summary>
    public virtual string Profile_I_Have_Been_Informed_About_The_Processing_Of_My_Personal_Data_And_I_Consent_To_It_As_Specifically_Defined => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_I_have_been_informed_about_the_processing_of_my_personal_data_and_I_consent_to_it__as_specifically_defined);

    /// <summary>
    ///   Gets the localized string for "Language".
    /// </summary>
    public virtual string Profile_Language => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Language);

    /// <summary>
    ///   Gets the localized string for "Last name".
    /// </summary>
    public virtual string Profile_Last_Name => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Last_name);

    /// <summary>
    ///   Gets the localized string for "Phone number".
    /// </summary>
    public virtual string Profile_Phone_Number => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Phone_number);

    /// <summary>
    ///   Gets the localized string for "Preferences".
    /// </summary>
    public virtual string Profile_Preferences => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Preferences);

    /// <summary>
    ///   Gets the localized string for "Profile".
    /// </summary>
    public virtual string Profile_Profile => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Profile);

    /// <summary>
    ///   Gets the localized string for "Remove".
    /// </summary>
    public virtual string Profile_Remove => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Remove);

    /// <summary>
    ///   Gets the localized string for "Save".
    /// </summary>
    public virtual string Profile_Save => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Save);

    /// <summary>
    ///   Gets the localized string for "Tax identification".
    /// </summary>
    public virtual string Profile_Tax_Identification => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Tax_identification);

    /// <summary>
    ///   Gets the localized string for "Unknown".
    /// </summary>
    public virtual string Profile_Unknown => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Unknown);

    /// <summary>
    ///   Gets the localized string for "Username".
    /// </summary>
    public virtual string Profile_Username => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Username);

    /// <summary>
    ///   Gets the localized string for "Your new email verification is still pending.".
    /// </summary>
    public virtual string Profile_Your_New_Email_Verification_Is_Still_Pending => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Your_new_email_verification_is_still_pending_);

    /// <summary>
    ///   Gets the localized string for "Your profile was updated successfully.".
    /// </summary>
    public virtual string Profile_Your_Profile_Was_Updated_Successfully => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_Your_profile_was_updated_successfully_);

    #endregion



    #region Register

    /// <summary>
    /// Text for "Associate your" phrase.
    /// </summary>
    public virtual string Register_Associate_your =>
        string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Associate_your);

    /// <summary>
    /// Text for "account" phrase.
    /// </summary>
    public virtual string Register_account =>
        string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_account);

    /// <summary>
    /// Constructs the full "Associate your {Provider} account" message.
    /// </summary>
    /// <param name="provider">Name of the external provider (e.g., Google, Facebook).</param>
    public virtual string Register_Associate_your_Account(string provider) =>
        string.Format(CultureInfo.CurrentUICulture, "{0} {1} {2}",
            IdentityLabels.Register_Associate_your,
            provider,
            IdentityLabels.Register_account);

    /// <summary>
    /// Text for "Register" button or label.
    /// </summary>
    public virtual string Register_Register => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Register);


    /// <summary>
    /// Text for "I have read and accept the Terms of service and privacy policy" on the Register page.
    /// </summary>
    public virtual string Register_I_Have_Read_And_Accept_Terms => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_I_Have_Read_And_Accept_Terms);

    /// <summary>
    /// Instruction shown at the top of the registration form explaining what the user needs to do.
    /// </summary>
    public virtual string Register_To_register_as_a_new_user_you_will_need_to_fill_in_the_following_information =>  string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_To_register_as_a_new_user_you_will_need_to_fill_in_the_following_information);

    /// <summary>
    ///   Gets the localized string for "Already have an account?".
    /// </summary>
    public virtual string Register_Already_Have_An_Account => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Already_have_an_account_);

    /// <summary>
    ///   Gets the localized string for "Calling Code".
    /// </summary>
    public virtual string Register_Calling_Code => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Calling_Code);

    /// <summary>
    ///   Gets the localized string for "Choose a username and a password of your choice. You can periodically change your password or whenever you wish to.".
    /// </summary>
    public virtual string Register_Choose_A_Username_And_A_Password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Choose_a_username_and_a_password_of_your_choice__You_can_periodically_change_your_password_or_whenever_you_wish_to_);

    /// <summary>
    ///   Gets the localized string for "Choose an email and a password of your choice. You can periodically change your password or whenever you wish to.".
    /// </summary>
    public virtual string Register_Choose_An_Email_And_A_Password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Choose_an_email_and_a_password_of_your_choice__You_can_periodically_change_your_password_or_whenever_you_wish_to_);

    /// <summary>
    ///   Gets the localized string for "First name".
    /// </summary>
    public virtual string Register_First_Name => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_First_name);

    /// <summary>
    ///   Gets the localized string for "here".
    /// </summary>
    public virtual string Register_Here => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_here);

    /// <summary>
    /// Text for "login with" label on Register page.
    /// </summary>
    public virtual string Register_login_with => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_login_with);

    /// <summary>
    ///   Gets the localized string for "I consent to the registration and processing of the above personal details for my contact and service as they are defined".
    /// </summary>
    public virtual string Register_I_Consent_To_Registration => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_I_consent_to_the_registration_and_processing_of_the_above_personal_details_for_my_contact_and_service_as_they_are_defined);

    /// <summary>
    ///   Gets the localized string for "I consent to the use of my contact information, including my email address, for the purpose of receiving commercial communications, promotional materials, and marketing updates from the organization. I understand that I may withdraw my consent at any time".
    /// </summary>
    public virtual string Register_I_Consent_To_Contact_Usage => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_I_consent_to_the_use_of_my_contact_information__including_my_email_address__for_the_purpose_of_receiving_commercial_communications__promotional_materials__and_marketing_updates_from_the_organization__I_understand_that_I_may_withdraw_my_consent_at_any_time_);

    /// <summary>
    ///   Gets the localized string for "Join us".
    /// </summary>
    public virtual string Register_Join_Us => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Join_us);

    /// <summary>
    ///   Gets the localized string for "Last name".
    /// </summary>
    public virtual string Register_Last_Name => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Last_name);

    /// <summary>
    ///   Gets the localized string for "OR".
    /// </summary>
    public virtual string Register_OR => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_OR);

    /// <summary>
    ///   Gets the localized string for "Password".
    /// </summary>
    public virtual string Register_Password => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Password);

    /// <summary>
    ///   Gets the localized string for "Phone number".
    /// </summary>
    public virtual string Register_Phone_number => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Phone_number);

    /// <summary>
    ///   Gets the localized string for "Sign up".
    /// </summary>
    public virtual string Register_Sign_Up => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Sign_up);

    /// <summary>
    ///   Gets the localized string for "These credentials are personal. Please remember them and do not reveal in any way (i.e orally, written, email) in third parties".
    /// </summary>
    public virtual string Register_Credentials_Notice => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_These_credentials_are_personal__Please_remember_them_and_do_not_reveal_in_any_way__i_e_orally__written__email__in_third_parties_);

    /// <summary>
    ///   Gets the localized string for "Timezone".
    /// </summary>
    public virtual string Register_Timezone => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Timezone);

    /// <summary>
    ///   Gets the localized string for "Username".
    /// </summary>
    public virtual string Register_Username => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Register_Username);

    #endregion

    #region SetLanguage


    /// <summary>
    /// Status message shown while the UI language is being set.
    /// </summary>
    public virtual string SetLanguage_Setting_ui_language => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.SetLanguage_Setting_ui_language);

    #endregion

    #region VerifyPhone

    /// <summary>
    ///   Gets the localized string for "Code".
    /// </summary>
    public virtual string VerifyPhone_Code => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.VerifyPhone_Code);

    /// <summary>
    ///   Gets the localized string for "Next".
    /// </summary>
    public virtual string VerifyPhone_Next => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.VerifyPhone_Next);

    /// <summary>
    ///   Gets the localized string for "Resend".
    /// </summary>
    public virtual string VerifyPhone_Resend => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.VerifyPhone_Resend);

    /// <summary>
    ///   Gets the localized string for "Save".
    /// </summary>
    public virtual string VerifyPhone_Save => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.VerifyPhone_Save);

    /// <summary>
    ///   Gets the localized string for "Verify phone number".
    /// </summary>
    public virtual string VerifyPhone_Verify_Phone_Number => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.VerifyPhone_Verify_phone_number);

    /// <summary>
    /// Text for "OTP is valid till:" label on the Verify Phone page.
    /// </summary>
    public virtual string VerifyPhone_OTP_is_valid_till => string.Format(CultureInfo.CurrentUICulture, IdentityLabels.VerifyPhone_OTP_is_valid_till);

    #endregion

}