﻿using Indice.Features.Identity.Core.Data.Models;
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
    /// <summary>Confirm your account.</summary>
    public virtual string UpdateEmailMessageSubject => string.Format(IdentityResources.Culture, IdentityResources.EmailUpdateMessageSubject);
    /// <summary>Email verification code is {0}.</summary>
    public virtual string UpdateEmailMessageBody<TUser>(TUser user, string token, string? returnUrl) where TUser : class => string.Format(IdentityResources.Culture, IdentityResources.EmailUpdateMessageBody, token);
    /// <summary>Forgot password.</summary>
    public virtual string ForgotPasswordMessageSubject => string.Format(IdentityResources.Culture, IdentityResources.ForgotPasswordMessageSubject);
    /// <summary>Email verification code is {0}.</summary>
    public virtual string ForgotPasswordMessageBody<TUser>(TUser user, string token) where TUser : class => string.Format(IdentityResources.Culture, IdentityResources.ForgotPasswordMessageBody, token);
    /// <summary>Your password is very common to use.</summary>
    public virtual string PasswordIsCommon => string.Format(IdentityResources.Culture, IdentityResources.PasswordIsCommon);
    /// <summary>Your password is identical to your username.</summary>
    public virtual string PasswordIdenticalToUserName => string.Format(IdentityResources.Culture, IdentityResources.PasswordIdenticalToUserName);
    /// <summary>This password has been used recently.</summary>
    public virtual string PasswordRecentlyUsed => string.Format(IdentityResources.Culture, IdentityResources.PasswordRecentlyUsed);
    /// <summary>Password cannot contain non Latin characters.</summary>
    public virtual string PasswordHasNonLatinChars => string.Format(IdentityResources.Culture, IdentityResources.PasswordHasNonLatinChars);
    /// <summary>Your password is very easy to guess, please choose a more complex one.</summary>
    public virtual string PasswordIsCommonRequirement => string.Format(IdentityResources.Culture, IdentityResources.PasswordIsCommonRequirement);
    /// <summary>Your password looks a lot like your username which can lead to your account been hacked.</summary>
    public virtual string PasswordIdenticalToUserNameRequirement => string.Format(IdentityResources.Culture, IdentityResources.PasswordIdenticalToUserNameRequirement);
    /// <summary>It is a good practice not to re-use your past password.</summary>
    public virtual string PasswordRecentlyUsedRequirement => string.Format(IdentityResources.Culture, IdentityResources.PasswordRecentlyUsedRequirement);
    /// <summary>Your password cannot contain non-Latin characters, which is required by the password policy.</summary>
    public virtual string PasswordHasNonLatinCharsRequirement => string.Format(IdentityResources.Culture, IdentityResources.PasswordHasNonLatinCharsRequirement);
    /// <summary>Your password contains not allowed characters.</summary>
    public virtual string PasswordContainsNotAllowedChars => string.Format(IdentityResources.Culture, IdentityResources.PasswordContainsNotAllowedChars);
    /// <summary>Not allowed characters.</summary>
    public virtual string PasswordContainsNotAllowedCharsRequirement => string.Format(IdentityResources.Culture, IdentityResources.PasswordContainsNotAllowedCharsRequirement);
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
}
