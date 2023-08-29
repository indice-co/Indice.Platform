using System.Security;
using Indice.Features.Identity.Core.TokenProviders;

namespace Indice.Features.Identity.Core.Configuration;

/// <summary>Configuration used in <see cref="Rfc6238AuthenticationService"/> service.</summary>
public class TotpOptions : TotpOptionsBase
{
    /// <summary>The name is used to mark the section found inside a configuration file.</summary>
    public static readonly string Name = "Totp";
    /// <summary>Enables saving standard OTPs in a certain claim (used mainly for development purposes). Defaults to false.</summary>
    public bool EnableDeveloperTotp { get; set; } = false;
}

/// <summary>Configuration used in <see cref="ExtendedPhoneNumberTokenProvider{TUser}"/> service.</summary>
public class PhoneNumberTokenProviderTotpOptions : TotpOptionsBase 
{
    /// <summary>The name is used to mark the section found inside a configuration file.</summary>
    public static readonly string Name = "Totp:PhoneNumber";
}

/// <summary>Configuration used in <see cref="ExtendedEmailTokenProvider{TUser}"/> service.</summary>
public class EmailTokenProviderTotpOptions : TotpOptionsBase
{
    /// <summary>The name is used to mark the section found inside a configuration file.</summary>
    public static readonly string Name = "Totp:Email";
}

/// <summary>Base options for Time-Base One Time Passwords.</summary>
public abstract class TotpOptionsBase 
{
    /// <summary>The default code duration in minutes.</summary>
    public const int DefaultCodeDuration = 2;
    /// <summary>The default code length;</summary>
    public const int DefaultCodeLength = 6;
    /// <summary>Specifies the duration in seconds in which the one-time password is valid. Default is 1 minute. For security reasons this value cannot exceed 6 minutes.</summary>
    public int CodeDuration { get; set; } = DefaultCodeDuration;
    /// <summary>An interval which will be used to calculate the value of the validity window.</summary>
    public double Timestep => CodeDuration / 2.0;
    /// <summary>Indicates the length of the OTP code. Defaults to 6.</summary>
    public int CodeLength { get; set; } = DefaultCodeLength;
}