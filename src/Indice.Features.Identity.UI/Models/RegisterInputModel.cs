namespace Indice.Features.Identity.UI.Models;

/// <summary>The register input model.</summary>
public class RegisterInputModel {
    /// <summary>The first name.</summary>
    public string? FirstName { get; set; }
    /// <summary>The last name.</summary>
    public string? LastName { get; set; }
    /// <summary>The username that will be used.</summary>
    public string? UserName { get; set; }
    /// <summary>The password.</summary>
    public string? Password { get; set; }
    /// <summary>The password confirmed (optional).</summary>
    public string? PasswordConfirmation { get; set; }
    /// <summary>The users email.</summary>
    public string? Email { get; set; }
    /// <summary>The users phone number. Usually used to store the mobile phone in order later on enable 2 factor authentication scenarios through SMS.</summary>
    public string? PhoneNumber { get; set; }
    /// <summary>The return URL is used to keep track of the original intent of the user when he landed on login and switched over to register.</summary>
    public string? ReturnUrl { get; set; }
    /// <summary>The privacy policy is read.</summary>
    public bool HasReadPrivacyPolicy { get; set; }
    /// <summary>The terms and conditions have been accepted.</summary>
    public bool HasAcceptedTerms { get; set; }
    /// <summary>List of claims where each item is formatted as claimType:claimValue collection of strings.</summary>
    public List<AttributeModel> Claims { get; set; } = new List<AttributeModel>();
    /// <summary>The id of the current client in the request. </summary>
    public string? ClientId { get; set; }
    /// <summary>The users time zone info.</summary>
    public string? ZoneInfo { get; set; }
    /// <summary> The Phone Number's Calling Code.</summary>
    public string? CallingCode { get; set; }

    /// <summary>Replace claim.</summary>
    /// <param name="name">Claim name.</param>
    /// <param name="value">Claim value.</param>
    protected void ReplaceClaim(string name, string value) {
        Claims.RemoveAll(x => x.Name.Equals(name));
        Claims.Add(new AttributeModel { 
            Name = name, 
            Value = value 
        });
    }
}
