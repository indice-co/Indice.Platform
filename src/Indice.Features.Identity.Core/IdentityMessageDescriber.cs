using System.Diagnostics;
using Humanizer;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Grants;

namespace Indice.Features.Identity.Core;

/// <summary>Provides an extensibility point for altering localized resources used inside the platform.</summary>
public class IdentityMessageDescriber
{
    /// <summary>User already has email '0'.</summary>
    /// <param name="email">The email address.</param>
    public virtual string EmailAlreadyExists(string email) => string.Format(IdentityResources.Culture, IdentityResources.UserAlreadyHasEmail, email);
    /// <summary>User's email is already confirmed.</summary>
    public virtual string EmailAlreadyConfirmed => string.Format(IdentityResources.Culture, IdentityResources.EmailAlreadyConfirmed);
    /// <summary>User already has phone number '{0}'.</summary>
    /// <param name="phoneNumber">The phone number.</param>
    public virtual string UserAlreadyHasPhoneNumber(string phoneNumber) => string.Format(IdentityResources.Culture, IdentityResources.UserAlreadyHasPhoneNumber, phoneNumber);
    /// <summary>User's phone number is already confirmed.</summary>
    public virtual string PhoneNumberAlreadyConfirmed => string.Format(IdentityResources.Culture, IdentityResources.PhoneNumberAlreadyConfirmed);
    /// <summary>SMS verification code is {token}.</summary>
    /// <param name="token">Phone number verification token.</param>
    public virtual string PhoneNumberVerificationMessage(string token) => string.Format(IdentityResources.Culture, IdentityResources.PhoneNumberVerificationMessage, token);
    /// <summary>SMS verification code is {token}.</summary>
    /// <param name="token">Phone number change verification token.</param>
    public virtual string PhoneNumberChangeVerificationMessage(string token) => string.Format(IdentityResources.Culture, IdentityResources.PhoneNumberChangeVerificationMessage, token);
    /// <summary>Confirm your account.</summary>
    public virtual string UpdateEmailMessageSubject => string.Format(IdentityResources.Culture, IdentityResources.EmailUpdateMessageSubject);
    /// <summary>Email verification code is {0}.</summary>
    public virtual string UpdateEmailMessageBody<TUser>(TUser user, string token, string? returnUrl) where TUser : class => string.Format(IdentityResources.Culture, IdentityResources.EmailUpdateMessageBody, token);
    /// <summary>Forgot password.</summary>
    public virtual string ForgotPasswordMessageSubject => string.Format(IdentityResources.Culture, IdentityResources.ForgotPasswordMessageSubject);
    /// <summary>Email verification code is {0}.</summary>
    public virtual string ForgotPasswordMessageBody<TUser>(TUser user, string token, string? confirmationUrl) where TUser : class => string.Format(IdentityResources.Culture, IdentityResources.ForgotPasswordMessageBody, confirmationUrl);
    /// <summary>Subject sent on message when <see cref="OtpAuthenticateExtensionGrantValidator"/> is used.</summary>
    public virtual string OtpSecuredValidatorOtpSubject => string.Format(IdentityResources.Culture, IdentityResources.OtpSecuredValidatorOtpSubject);
    /// <summary>Message sent on message when <see cref="OtpAuthenticateExtensionGrantValidator"/> is used. Should contain the '{0}' placeholder for the generated token.</summary>
    public virtual string OtpSecuredValidatorOtpBody() => string.Format(IdentityResources.Culture, IdentityResources.OtpSecuredValidatorOtpBody, "{0}");
    /// <summary>Registration OTP code for device {0} is {1}.</summary>
    public virtual string DeviceRegistrationCodeMessage(string? deviceName, InteractionMode interactionMode) => string.Format(IdentityResources.Culture, IdentityResources.DeviceRegistrationOtpCode, deviceName, "{0}");
    /// <summary>User cannot add a device because the limit is reached.</summary>
    public virtual string MaxNumberOfDevices() => string.Format(IdentityResources.Culture, IdentityResources.MaxNumberOfDevices);
    /// <summary>User tries to set the number of allowed devices to a value greater than the allowed one.</summary>
    public virtual string LargeNumberOfDevices(int userMaxDevicesCount, int maxAllowedRegisteredDevices) => string.Format(IdentityResources.Culture, IdentityResources.LargeNumberOfDevices, userMaxDevicesCount, maxAllowedRegisteredDevices);
    /// <summary>User tries to set the number of allowed devices to a value lower than the current number.</summary>
    public virtual string LargeNumberOfUserDevices(int userDevicesCount, int maxAllowedRegisteredDevices) => string.Format(IdentityResources.Culture, IdentityResources.LargeNumberOfUserDevices, userDevicesCount, maxAllowedRegisteredDevices);
    /// <summary>User tries to set the number of allowed devices to a value lower than 1.</summary>
    public virtual string InsufficientNumberOfDevices() => string.Format(IdentityResources.Culture, IdentityResources.InsufficientNumberOfDevices);
    /// <summary>Device is pending trust activation.</summary>
    public virtual string DevicePendingTrustActivation() => string.Format(IdentityResources.Culture, IdentityResources.DevicePendingTrustActivation);
    /// <summary>User cannot add any other trusted devices.</summary>
    public virtual string TrustedDevicesLimitReached() => string.Format(IdentityResources.Culture, IdentityResources.TrustedDevicesLimitReached);
    /// <summary>Device is already trusted.</summary>
    public virtual string DeviceAlreadyTrusted() => string.Format(IdentityResources.Culture, IdentityResources.DeviceAlreadyTrusted);
    /// <summary>Message content when <strong>RequiresOtpAttribute</strong> is used.</summary>
    public virtual string RequiresOtpMessage() => IdentityResources.RequiresOtpMessage;
    /// <summary>Message content when <strong>TrustDeviceRequiresOtpAttribute</strong> is used.</summary>
    public virtual string TrustedDeviceRequiresOtpMessage(UserDevice device) => IdentityResources.TrustedDeviceRequiresOtpMessage;
    /// <summary>Message content for an invalid phone number format.</summary>
    public virtual string InvalidPhoneNumber() => IdentityResources.InvalidPhoneNumber;
    /// <summary>Message content for suspicious login attempt (Impossible Travel).</summary>
    public virtual string ImpossibleTravelOtpMessage() => string.Format(IdentityResources.Culture, IdentityResources.ImpossibleTravelOtpMessage, "{0}");
    /// <summary>Subject content for suspicious login attempt (Impossible Travel).</summary>
    public virtual string ImpossibleTravelOtpSubject => string.Format(IdentityResources.Culture, IdentityResources.ImpossibleTravelOtpSubject);
    /// <summary>Subject content for confirmation email.</summary>
    public virtual string RegisterEmailSubject(string applicationName) => string.Format(IdentityResources.Culture, IdentityResources.RegisterEmailSubject, applicationName);
    /// <summary>Subject content for confirmation email.</summary>
    public virtual string ConfirmationEmailSubject => string.Format(IdentityResources.Culture, IdentityResources.ConfirmationEmailSubject);
    /// <summary>Subject content for confirmation of email change .</summary>
    public virtual string ConfirmationEmailChangeSubject => string.Format(IdentityResources.Culture, IdentityResources.ConfirmationEmailChangeSubject);

    /// <summary>Email verification code is {0}.</summary>
    public virtual string ChangeEmailMessageBody<TUser>(TUser user, string token, string newEmail, string? returnUrl) where TUser : class => string.Format(IdentityResources.Culture, IdentityResources.EmailChangeMessageBody, token);
    /// <summary>OTP Subject for phone update confirmation.</summary>
    public virtual string PhoneVerificationSmsSubject => string.Format(IdentityResources.Culture, IdentityResources.PhoneVerificationSmsSubject);
    /// <summary>OTP Subject for phone change confirmation.</summary>
    public virtual string PhoneChangeVerificationSmsSubject => string.Format(IdentityResources.Culture, IdentityResources.PhoneChangeVerificationSmsSubject);
    /// <summary>OTP body for phone confirmation.</summary>
    public virtual string PhoneVerificationSmsBody(string code) => string.Format(IdentityResources.Culture, IdentityResources.PhoneVerificationSmsBody, code);

    /// <summary>Security event subject.</summary>
    public virtual string SecurityEventSubject(string activity) =>
        activity switch {
            nameof(PasswordChangedEvent) => IdentityResources.PasswordChangedEventSubject,
            nameof(AccountLockedEvent) => IdentityResources.AccountLockedEventSubject,
            _ => string.Format(IdentityResources.Culture, IdentityResources.SecurityNotificationDefaultSubject, activity.Replace("Event", "").Humanize())
        };

    /// <summary>Security event descriptions.</summary>
    public virtual string SecurityEventDescription(string activity) =>
        activity switch {
            nameof(PasswordChangedEvent) => IdentityResources.PasswordChangedEventDescription,
            nameof(AccountLockedEvent) => IdentityResources.AccountLockedEventDescription,
            _ => string.Empty
        };

    /// <summary>Add email page validation empty email</summary>
    public virtual string AddEmailValidationEmailEmpty => IdentityResources.AddEmailValidationEmailEmpty;

    /// <summary>Add email page confirmation email send message</summary>
    public virtual string AddEmailConfirmationEmailSend => IdentityResources.AddEmailConfirmationEmailSend;

    /// <summary>Add phone page validation phone empty</summary>
    public virtual string AddPhoneValidationPhoneEmpty => IdentityResources.AddPhoneValidationPhoneEmpty;

    /// <summary>Forgot password confirmation error</summary>
    public virtual string ForgotPasswordConfirmationError => IdentityResources.ForgotPasswordConfirmationError;

    /// <summary>Login page validation invalid credentials error</summary>
    public virtual string LoginValidationInvalidCredentials => IdentityResources.LoginValidationInvalidCredentials;
    /// <summary>Mfa onboarding AddPhone Validation Phone Empty</summary>
    public virtual string MfaAddPhoneValidationPhoneEmpty => IdentityResources.MfaAddPhoneValidationPhoneEmpty;
    /// <summary>Mfa onboarding AddEmail Validation Email Already Confirmed</summary>
    public virtual string MfaAddEmailValidationEmailAlreadyConfirmed => IdentityResources.MfaAddEmailValidationEmailAlreadyConfirmed;
    /// <summary>Mfa onboarding AddEmail Validation Email Empty</summary>
    public virtual string MfaAddEmailValidationEmailEmpty => IdentityResources.MfaAddEmailValidationEmailEmpty;

    /// <summary>Mfa onboarding AddPhone Validation success message</summary>
    public virtual string MfaAddEmailSuccessMessage => IdentityResources.MfaAddEmailSuccessMessage;
    /// <summary>Mfa onboarding AddPhone Validation success message</summary>
    public virtual string MfaAddPhoneSuccessMessage => IdentityResources.MfaAddPhoneSuccessMessage;
    /// <summary>Mfa onboarding AddPhone Validation already confirmed</summary>
    public virtual string MfaAddPhoneValidationPhoneAlreadyConfirmed => IdentityResources.MfaAddPhoneValidationPhoneAlreadyConfirmed;
    /// <summary>Mfa onboarding AddPhone Validation missing phone</summary>
    public virtual string MfaVerifyPhoneValidationMissingPhone => IdentityResources.MfaVerifyPhoneValidationMissingPhone;

    /// <summary>Mfa onboarding email Validation missing email</summary>
    public virtual string MfaVerifyEmailValidationMissingEmail => IdentityResources.MfaVerifyEmailValidationMissingEmail;

    /// <summary>Mfa onboarding verfication success message</summary>
    public virtual string MfaVerifyPhoneSuccessMessage => IdentityResources.MfaVerifyPhoneSuccessMessage;

    /// <summary>Mfa onboarding email verification success message</summary>
    public virtual string MfaVerifyEmailSuccessMessage => IdentityResources.MfaVerifyEmailSuccessMessage;
    /// <summary>Password changed successfully message.</summary>
    public virtual string PasswordChangedSuccessfully => IdentityResources.PasswordChangedSuccessfully;
    /// <summary>Password expired message.</summary>
    public virtual string PasswordExpiredMessage => IdentityResources.PasswordExpiredMessage;

    /// <summary>Choose Password message for new users.</summary>
    public virtual string PasswordExpiredFirstTimeUserMessage => IdentityResources.PasswordExpiredFirstTimeUserMessage;

    /// <summary>Profile external login added success message.</summary>
    public virtual string ProfileExternalLoginAddedSuccessMessage => IdentityResources.ProfileExternalLoginAddedSuccessMessage;
    /// <summary>Registration phone confriamtion message prompt</summary>
    public virtual string RegisterPhoneConfirmationPrompt => IdentityResources.RegisterPhoneConfirmationPrompt;

    /// <summary>Mfa message subject</summary>
    public virtual string MfaSmsSubject => IdentityResources.MfaSmsSubject;
    /// <summary>Mfa message subject</summary>
    public virtual string MfaSmsBody => IdentityResources.MfaSmsBody;

    /// <summary>Mfa email subject</summary>
    public virtual string MfaEmailSubject => IdentityResources.MfaEmailSubject;
    /// <summary>Mfa email body</summary>
    public virtual string MfaEmailBody => IdentityResources.MfaEmailBody;

    /// <summary>Mfa validation error message</summary>
    public virtual string MfaValidationError => IdentityResources.MfaValidationError;
    /// <summary>Login error message when user is locked out.</summary>
    public virtual string LoginErrorLockedMessage => IdentityResources.LoginErrorLockedMessage;

    #region Add Password UI
    /// <summary>Gets the field name used for validating the "New Password" input in the Add Password UI.</summary>
    public virtual string UI_Validator_AddPassword_NewPassword_FieldName => IdentityResources.UI_Validator_AddPassword_NewPassword_FieldName;
    /// <summary>Gets the field name used for the "Confirm Password" validation in the Add Password UI.</summary>
    public virtual string UI_Validator_AddPassword_ConfirmPassword_FieldName => IdentityResources.UI_Validator_AddPassword_ConfirmPassword_FieldName;
    #endregion

    #region Password Expired UI
    /// <summary>The message displayed when the new password field is empty in the Password Expired UI.</summary>
    public virtual string UI_Validator_PasswordExpired_NewPassword_Empty_Error => IdentityResources.UI_Validator_PasswordExpired_NewPassword_Empty_Error;
    /// <summary>The message displayed when the new password confirmation field is empty in the Password Expired UI.</summary>
    public virtual string UI_Validator_PasswordExpired_NewPasswordConfirmation_Empty_Error => IdentityResources.UI_Validator_PasswordExpired_NewPasswordConfirmation_Empty_Error;
    /// <summary>The message displayed when the new password confirmation does not match the new password in the Password Expired UI.</summary>
    public virtual string UI_Validator_PasswordExpired_NewPasswordConfirmation_Mismatch_Error => IdentityResources.UI_Validator_PasswordExpired_NewPasswordConfirmation_Mismatch_Error;
    #endregion

    #region Add Email UI
    /// <summary>Gets the field name used for validating the "New Password" input in the Password Expired UI.</summary>
    public virtual string UI_Validator_AddEmail_Email_FieldName => IdentityResources.UI_Validator_AddEmail_Email_FieldName;
    #endregion

    #region Add Phone UI
    /// <summary>Gets the field name used for validating the "Phone Number" input in the Add Phone UI.</summary>
    public virtual string UI_Validator_AddPhone_CallingCode_FieldName => IdentityResources.UI_Validator_AddPhone_CallingCode_FieldName;
    /// <summary>Gets the field name used for validating the "Phone Number" input in the Add Phone UI.</summary>
    public virtual string UI_Validator_AddPhone_PhoneNumber_FieldName => IdentityResources.UI_Validator_AddPhone_PhoneNumber_FieldName;
    /// <summary>Gets the error message displayed when the phone number format is invalid in the Add Phone UI.</summary>
    public virtual string UI_Validator_AddPhone_PhoneNumber_InvalidFormat => IdentityResources.UI_Validator_AddPhone_PhoneNumber_InvalidFormat;
    #endregion

    #region Change Password UI
    /// <summary>Gets the field name used for validating the "Old Password" input in the Change Password UI.</summary>
    public virtual string UI_Validator_ChangePassword_OldPassword_FieldName => IdentityResources.UI_Validator_ChangePassword_OldPassword_FieldName;
    /// <summary>Gets the field name used for validating the "New Password" input in the Change Password UI.</summary>
    public virtual string UI_Validator_ChangePassword_NewPassword_FieldName => IdentityResources.UI_Validator_ChangePassword_NewPassword_FieldName;
    #endregion

    #region Enable Mfa Sms UI
    /// <summary>Gets the field name used for validating the "Phone Number" input in the Enable MFA SMS UI.</summary>
    public virtual string UI_Validator_EnableMfaSms_PhoneNumber_FieldName => IdentityResources.UI_Validator_EnableMfaSms_PhoneNumber_FieldName;
    /// <summary>Gets the error message displayed when the phone number format is invalid in the Enable MFA SMS UI.</summary>
    public virtual string UI_Validator_EnableMfaSms_PhoneNumber_InvalidFormat => IdentityResources.UI_Validator_EnableMfaSms_PhoneNumber_InvalidFormat;
    #endregion

    #region Forgot Password Confirmation UI
    /// <summary>Gets the field name used for validating the "Email" input in the Forgot Password Confirmation UI.</summary>
    public virtual string UI_Validator_ForgotPasswordConfirmation_Email_FieldName => IdentityResources.UI_Validator_ForgotPasswordConfirmation_Email_FieldName;
    /// <summary>Gets the field name used for validating the "New Password" input in the Forgot Password Confirmation UI.</summary>
    public virtual string UI_Validator_ForgotPasswordConfirmation_NewPassword_FieldName => IdentityResources.UI_Validator_ForgotPasswordConfirmation_NewPassword_FieldName;
    /// <summary>Gets the field name used for validating the "Token" input in the Forgot Password Confirmation UI.</summary>
    public virtual string UI_Validator_ForgotPasswordConfirmation_Token_FieldName => IdentityResources.UI_Validator_ForgotPasswordConfirmation_Token_FieldName;
    #endregion

    #region Forgot Password UI
    /// <summary>Gets the field name used for validating the "Email" input in the Forgot Password UI.</summary>
    public virtual string UI_Validator_ForgotPassword_Email_FieldName => IdentityResources.UI_Validator_ForgotPassword_Email_FieldName;
    #endregion

    #region Login UI
    /// <summary>Gets the field name used for validating the "UserName" input in the Login UI.</summary>
    public virtual string UI_Validator_Login_UserName_FieldName => IdentityResources.UI_Validator_Login_UserName_FieldName;
    /// <summary>Gets the field name used for validating the "Password" input in the Login UI.</summary>
    public virtual string UI_Validator_Login_Password_FieldName => IdentityResources.UI_Validator_Login_Password_FieldName;
    #endregion

    #region Mfa Onboarding UI
    /// <summary>Gets the field name used for validating the "SelectedAuthenticationMethod" input in the Mfa Onboarding UI.</summary>
    public virtual string UI_Validator_MfaOnboarding_SelectedAuthenticationMethod_FieldName => IdentityResources.UI_Validator_MfaOnboarding_SelectedAuthenticationMethod_FieldName;
    /// <summary>Gets the error message displayed when the "SelectedAuthenticationMethod" input is required in the Mfa Onboarding UI.</summary>
    public virtual string UI_Validator_MfaOnboarding_SelectedAuthenticationMethod_Required => IdentityResources.UI_Validator_MfaOnboarding_SelectedAuthenticationMethod_Required;
    #endregion

    #region Profile UI
    /// <summary>Gets the field name used for validating the "UserName" input in the Profile UI.</summary>
    public virtual string UI_Validator_Profile_UserName_FieldName => IdentityResources.UI_Validator_Profile_UserName_FieldName;
    /// <summary>Gets the error message displayed when the "UserName" input is invalid in the Profile UI.</summary>
    public virtual string UI_Validator_Profile_UserName_InvalidFormat => IdentityResources.UI_Validator_Profile_UserName_InvalidFormat;
    /// <summary>Gets the field name used for validating the "Email" input in the Profile UI.</summary>
    public virtual string UI_Validator_Profile_Email_FieldName => IdentityResources.UI_Validator_Profile_Email_FieldName;
    /// <summary>Gets the field name used for validating the "CallingCode" input in the Profile UI.</summary>
    public virtual string UI_Validator_Profile_CallingCode_FieldName => IdentityResources.UI_Validator_Profile_CallingCode_FieldName;
    /// <summary>Gets the field name used for validating the "PhoneNumberWithCallingCode" input in the Profile UI.</summary>
    public virtual string UI_Validator_Profile_PhoneNumber_FieldName => IdentityResources.UI_Validator_Profile_PhoneNumber_FieldName;
    /// <summary>Gets the error message displayed when the "PhoneNumberWithCallingCode" input is invalid in the Profile UI.</summary>
    public virtual string UI_Validator_Profile_PhoneNumber_InvalidFormat => IdentityResources.UI_Validator_Profile_PhoneNumber_InvalidFormat;
    /// <summary>Gets the field name used for validating the "Tin" input in the Profile UI.</summary>
    public virtual string UI_Validator_Profile_Tin_FieldName => IdentityResources.UI_Validator_Profile_Tin_FieldName;
    /// <summary>Gets the error message displayed when the "Tin" input is invalid in the Profile UI.</summary>
    public virtual string UI_Validator_Profile_Tin_InvalidFormat => IdentityResources.UI_Validator_Profile_Tin_InvalidFormat;
    #endregion

    #region Register UI
    /// <summary>Gets the field name used for validating the "FirstName" input in the Register UI.</summary>
    public virtual string UI_Validator_Register_FirstName_FieldName => IdentityResources.UI_Validator_Register_FirstName_FieldName;
    /// <summary>Gets the field name used for validating the "LastName" input in the Register UI.</summary>
    public virtual string UI_Validator_Register_LastName_FieldName => IdentityResources.UI_Validator_Register_LastName_FieldName;
    /// <summary>Gets the field name used for validating the "UserName" input in the Register UI.</summary>
    public virtual string UI_Validator_Register_UserName_FieldName => IdentityResources.UI_Validator_Register_UserName_FieldName;
    /// <summary>Gets the error message displayed when the "UserName" input is invalid in the Register UI.</summary>
    public virtual string UI_Validator_Register_UserName_InvalidFormat => IdentityResources.UI_Validator_Register_UserName_InvalidFormat;
    /// <summary>Gets the error message displayed when the "UserName" already exists.</summary>
    public virtual string UI_Validator_Register_UserName_AlreadyExists => IdentityResources.UI_Validator_Register_UserName_AlreadyExists;
    /// <summary>Gets the field name used for validating the "Password" input in the Register UI.</summary>
    public virtual string UI_Validator_Register_Password_FieldName => IdentityResources.UI_Validator_Register_Password_FieldName;
    /// <summary>Gets the field name used for validating the "Email" input in the Register UI.</summary>
    public virtual string UI_Validator_Register_Email_FieldName => IdentityResources.UI_Validator_Register_Email_FieldName;
    /// <summary>Gets the error message displayed when the "Email" already exists.</summary>
    public virtual string UI_Validator_Register_Email_AlreadyExists => IdentityResources.UI_Validator_Register_Email_AlreadyExists;
    /// <summary>Gets the error message displayed when the "Terms" are not accepted in the Register UI.</summary>
    public virtual string UI_Validator_Register_AcceptTerms_Message => IdentityResources.UI_Validator_Register_AcceptTerms_Message;
    /// <summary>Gets the error message displayed when the "Privacy Policy" is not read in the Register UI.</summary>
    public virtual string UI_Validator_Register_ReadPrivacyPolicy_Message => IdentityResources.UI_Validator_Register_ReadPrivacyPolicy_Message;
    /// <summary>Gets the field name used for validating the "CallingCode" input in the Register UI.</summary>
    public virtual string UI_Validator_Register_CallingCode_FieldName => IdentityResources.UI_Validator_Register_CallingCode_FieldName;
    /// <summary>Gets the error message displayed when the "PhoneNumberWithCallingCode" input is invalid in the Register UI.</summary>
    public virtual string UI_Validator_Register_PhoneNumber_InvalidFormat => IdentityResources.UI_Validator_Register_PhoneNumber_InvalidFormat;
    #endregion

    #region Verify Phone UI
    /// <summary>Gets the field name used for validating the "PhoneNumber" input in the Verify Phone UI.</summary>
    public virtual string UI_Validator_VerifyPhone_PhoneNumber_FieldName => IdentityResources.UI_Validator_VerifyPhone_PhoneNumber_FieldName;
    /// <summary>Gets the error message displayed when the "PhoneNumber" input is invalid in the Verify Phone UI.</summary>
    public virtual string UI_Validator_VerifyPhone_PhoneNumber_InvalidFormat => IdentityResources.UI_Validator_VerifyPhone_PhoneNumber_InvalidFormat;
    /// <summary>Gets the field name used for validating the "Code" input in the Verify Phone UI.</summary>
    public virtual string UI_Validator_VerifyPhone_Code_FieldName => IdentityResources.UI_Validator_VerifyPhone_Code_FieldName;
    #endregion

    #region Authentication Methods
    /// <summary>Display name for SMS authentication method.</summary>
    public virtual string AuthMethod_Sms_DisplayName => string.Format(IdentityResources.Culture, IdentityResources.AuthMethod_Sms_DisplayName);
    /// <summary>Description for SMS authentication method.</summary>
    public virtual string AuthMethod_Sms_Description => string.Format(IdentityResources.Culture, IdentityResources.AuthMethod_Sms_Description);

    /// <summary>Display name for Email authentication method.</summary>
    public virtual string AuthMethod_Email_DisplayName => string.Format(IdentityResources.Culture, IdentityResources.AuthMethod_Email_DisplayName);
    /// <summary>Description for Email authentication method.</summary>
    public virtual string AuthMethod_Email_Description => string.Format(IdentityResources.Culture, IdentityResources.AuthMethod_Email_Description);

    /// <summary>Display name for Authenticator App authentication method.</summary>
    public virtual string AuthMethod_AuthenticatorApp_DisplayName => string.Format(IdentityResources.Culture, IdentityResources.AuthMethod_AuthenticatorApp_DisplayName);
    /// <summary>Description for Authenticator App authentication method.</summary>
    public virtual string AuthMethod_AuthenticatorApp_Description => string.Format(IdentityResources.Culture, IdentityResources.AuthMethod_AuthenticatorApp_Description);

    /// <summary>Display name for FIDO2 authentication method.</summary>
    public virtual string AuthMethod_Fido2_DisplayName => string.Format(IdentityResources.Culture, IdentityResources.AuthMethod_Fido2_DisplayName);
    /// <summary>Description for FIDO2 authentication method.</summary>
    public virtual string AuthMethod_Fido2_Description => string.Format(IdentityResources.Culture, IdentityResources.AuthMethod_Fido2_Description);

    /// <summary>Display name for Viber authentication method.</summary>
    public virtual string AuthMethod_Viber_DisplayName => string.Format(IdentityResources.Culture, IdentityResources.AuthMethod_Viber_DisplayName);
    /// <summary>Description for Viber authentication method.</summary>
    public virtual string AuthMethod_Viber_Description => string.Format(IdentityResources.Culture, IdentityResources.AuthMethod_Viber_Description);

    /// <summary>Display name for Trusted Device authentication method.</summary>
    public virtual string AuthMethod_TrustedDevice_DisplayName => string.Format(IdentityResources.Culture, IdentityResources.AuthMethod_TrustedDevice_DisplayName);
    /// <summary>Description for Trusted Device authentication method.</summary>
    public virtual string AuthMethod_TrustedDevice_Description => string.Format(IdentityResources.Culture, IdentityResources.AuthMethod_TrustedDevice_Description);
    #endregion
}