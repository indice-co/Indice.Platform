using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace Indice.AspNetCore.Authentication.GovGr;

/// <summary>
/// 
/// </summary>
public class GovGrOAuthOptions : OAuthOptions
{
    /// <summary>The OAuth endpoint used to exchange access tokens.</summary>
    public string? LogoutEndpoint { get; set; }
    /// <summary> Indicates whether federated logout is enabled.</summary>
    public bool EnableFederatedLogout { get; set; } = true;
}
