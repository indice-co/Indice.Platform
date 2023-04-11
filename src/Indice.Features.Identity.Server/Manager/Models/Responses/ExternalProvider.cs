using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>the external provider model</summary>
public class ExternalProvider
{
    /// <summary>The display name</summary>
    public string? DisplayName { get; set; }

    /// <summary>The authentication scheme for the cookie</summary>
    public string? AuthenticationScheme { get; set; }
}