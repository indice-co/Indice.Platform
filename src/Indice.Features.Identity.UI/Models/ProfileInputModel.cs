﻿using Indice.Globalization;

namespace Indice.Features.Identity.UI.Models;

/// <summary>Request input model for the manage profile page.</summary>
public class ProfileInputModel
{
    /// <summary></summary>
    public string? FirstName { get; set; }
    /// <summary></summary>
    public string? LastName { get; set; }
    /// <summary></summary>
    public string? UserName { get; set; }
    /// <summary></summary>
    public string? Email { get; set; }
    /// <summary></summary>
    public string? CallingCode { get; set; }
    /// <summary></summary>
    public string? PhoneNumber { get; set; }
    /// <summary></summary>
    public string? Tin { get; set; }
    /// <summary></summary>
    public DateTime? BirthDate { get; set; }
    /// <summary></summary>
    public bool ConsentCommercial { get; set; }
    /// <summary></summary>
    public DateTime? ConsentCommercialDate { get; set; }
    /// <summary></summary>
    public string? DeveloperTotp { get; set; }
    /// <summary></summary>
    public string? ZoneInfo { get; set; }

    /// <summary>A calculated field that holds the <see cref="PhoneNumber"/> padded with its international <seealso cref="CallingCode"/>.</summary>
    public string? PhoneNumberWithCallingCode => string.IsNullOrWhiteSpace(CallingCode) ? PhoneNumber : $"{CallingCode} {PhoneNumber}";

    /// <summary>Calculates a display name for UI purposes</summary>
    public string? DisplayName => string.IsNullOrWhiteSpace(FirstName) ? UserName : (FirstName + " " + LastName).Trim();
}
