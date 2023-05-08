using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Identity.UI.Pages;

namespace Indice.Features.Identity.UI;

/// <summary>
/// Configuration options for Identity UI
/// </summary>
public class IdentityUIOptions
{
    /// <summary>
    /// An absolute url to the <strong>terms and conditions</strong> web page. Use it when this page is located to (or shared with) an external website.
    /// </summary>
    /// <remarks>If left null the <strong>./legal/terms.md</strong> will be used. If populated it will do a redirect to this url</remarks>
    public string? TermsUrl { get; set; }
    /// <summary>
    /// An absolute url to the <strong>privacy</strong> web page. Use it when this page is located to (or shared with) an external website.
    /// </summary>
    /// <remarks>If left null the <strong>./legal/privacy.md</strong> will be used. If populated it will do a redirect to this url</remarks>
    public string? PrivacyUrl { get; set; }

    /// <summary>Controls whether an external Identity user will go through the associata screen or not</summary>
    public bool AutoProvisionExternalUsers { get; set; } = true;

    /// <summary>Controls whether an external Identity user be associated to an existing one using the email account</summary>
    public bool AutoAssociateExternalUsers { get; set; } = true;
}
