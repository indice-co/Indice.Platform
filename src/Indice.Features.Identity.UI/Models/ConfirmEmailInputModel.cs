using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.Models;

/// <summary>Confirm email input model</summary>
public class ConfirmEmailInputModel
{
    /// <summary>The user id</summary>
    [Required]
    public string UserId { get; set; } = string.Empty;
    /// <summary>The email verification token. Related to the email address.</summary>
    [Required]
    public string Token { get; set; } = string.Empty;
    /// <summary>An optional url to pass to the page so that someone can redirect after confirming to an external application</summary>
    /// <remarks>This should be verified</remarks>
    public string? ReturnUrl { get; set; }
    /// <summary>The client id is there to kick the theme to into play. The param is <strong>client_id</strong>. This works only if always there in the querystring</summary>
    [BindProperty(Name = "client_id")]
    public string? ClientId { get; set; }
    /// <summary>
    /// This is related to whether we want the glow to show that something whent wrong to the user or not. 
    /// Something went wrong meaning that we found the user but he is already confirmed,
    /// or the verification token is invalid. <strong>twc</strong>
    /// </summary>
    [BindProperty(Name = "twc")]
    public bool ThrowWhenConfirmed { get; set; } = false;
    /// <summary>Should redirect works in conjunction with the returnrul parameter. Querystring: <strong>sr</strong></summary>
    [BindProperty(Name = "sr")]
    public bool ShouldRedirect { get; set; } = false;
}
