using System;

namespace Indice.Security;

/// <summary>Common claim types used in all Indice applications.</summary>
public static class BasicClaimTypes
{
    /// <summary>Basic Claim types prefix.</summary>
    public const string Prefix = "indice_";
    /// <summary>Identifies a physical user principal as a trusted account with administrative privileges.</summary>
    public const string Admin = "admin";
    /// <summary>Identifies a machine (worker) principal as a trusted system account with administrative privileges.</summary>
    public const string System = "system";
    /// <summary>Identifier for the current tenant.</summary>
    public const string TenantId = "tenantId";
    /// <summary>Alternate key for the current tenant.</summary>
    public const string TenantAlias = "tenantAlias";
    /// <summary>User id.</summary>
    public const string Subject = "sub";
    /// <summary> User email.</summary>
    public const string Email = "email";
    /// End-User's preferred telephone number. E.164 (https://www.itu.int/rec/T-REC-E.164/e) is RECOMMENDED as the format of this Claim, for example, +1 (425) 555-1212 or
    /// +56 (2) 687 2400. If the phone number contains an extension, it is RECOMMENDED that the extension be represented using the RFC 3966 [RFC3966] extension syntax, for example, +1 (604) 555-1234;ext=5678.
    public const string PhoneNumber = "phone_number";
    /// <summary>User last name.</summary>
    public const string FamilyName = "family_name";
    /// <summary>User first name.</summary>
    public const string GivenName = "given_name";
    /// <summary>End-User's full name in displayable form including all name parts, possibly including titles and suffixes, ordered according to the End-User's locale and preferences.</summary>
    public const string Name = "name";
    /// <summary>Full name.</summary>
    public const string FullName = "full_name";
    /// <summary>String from the time zone database (http://www.twinsun.com/tz/tz-link.htm) representing the End-User's time zone. For example, Europe/Paris or America/Los_Angeles.</summary>
    public const string ZoneInfo = "zoneinfo";
    /// <summary>
    /// End-User's locale, represented as a BCP47 [RFC5646] language tag. This is typically an ISO 639-1 Alpha-2 [ISO639‑1] language code in lowercase and an ISO 3166-1
    /// Alpha-2 [ISO3166‑1] country code in uppercase, separated by a dash. For example, en-US or fr-CA. As a compatibility note, some implementations have used an underscore
    /// as the separator rather than a dash, for example, en_US; Relying Parties MAY choose to accept this locale syntax as well.
    /// </summary>
    public const string Locale = "locale";
    /// <summary>The role.</summary>
    public const string Role = "role";
    /// <summary>End-User's gender. Values defined by this specification are "female" and "male". Other values MAY be used when neither of the defined values are applicable.</summary>
    public const string Gender = "gender";
    /// <summary>
    /// End-User's birthday, represented as an ISO 8601:2004 [ISO8601‑2004] YYYY-MM-DD format. The year MAY be 0000, indicating that it is omitted. To represent only
    /// the year, YYYY format is allowed. Note that depending on the underlying platform's date related function, providing just year can result in varying month and day,
    /// so the implementers need to take this factor into account to correctly process the dates.
    /// </summary>
    public const string BirthDate = "birthdate";
    /// <summary>Client Id (calling application).</summary>
    public const string ClientId = "client_id";
    /// <summary>The <see cref="DateTime"/> when the user password will expire.</summary>
    public const string PasswordExpirationDate = "password_expiration_date";
    /// <summary>A boolean claim indicating that user password has expired.</summary>
    public const string PasswordExpired = "password_expired";
    /// <summary>Defines the period in which a password expires.</summary>
    public const string PasswordExpirationPolicy = "password_expiration_policy";
    /// <summary>Defines a standard OTP code used for bypassing OTP verification for development environment.</summary>
    public const string DeveloperTotp = "developer_totp";
    /// <summary>Commercial consent.</summary>
    public const string ConsentCommencial = "consent_commencial";
    /// <summary>Terms of service consent.</summary>
    public const string ConsentTerms = "consent_terms";
    /// <summary>Commercial consent date.</summary>
    public const string ConsentCommencialDate = "consent_commencial_date";
    /// <summary>Terms of service consent date.</summary>
    public const string ConsentTermsDate = "consent_terms_date";
    /// <summary>Trusted device authorized client.</summary>
    public const string TrustedDevice = "trusted_device";
    /// <summary>OTP is already verified.</summary>
    public const string OtpAuthenticated = "otp_authenticated";
    /// <summary>Tax identification number.</summary>
    public const string Tin = "tin";
    /// <summary>Access token obtained by Microsoft Identity Platform.</summary>
    public const string MsGraphToken = "ms_graph_access_token";
    /// <summary>The maximum number of devices a user can register.</summary>
    public const string MaxDevicesCount = "max_devices_count";
    /// <summary>Marks a client as a mobile client.</summary>
    public const string MobileClient = "mobile_client";
    /// <summary>All possible user related claims.</summary>
    public static readonly string[] UserClaims = {
        "sub",
        "name",
        "email",
        "phone",
        "phone_verified",
        "email_verified",
        "family_name",
        "given_name",
        "role",
        Admin,
        PasswordExpirationDate
    };
    /// <summary>The scopes.</summary>
    public const string Scope = "scope";
}
