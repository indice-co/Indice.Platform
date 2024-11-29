using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Security;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Server.Options;

/// <summary>The options for the initialization of the Case Api.</summary>
public class CaseServerEndpointOptions
{

    /// <summary>The default scope name to be used for Cases API. Defaults to <strong>cases</strong>.</summary>
    public string Scope { get; set; } = "cases";

    /// <summary>Cases GroupName Api Explorer. Defaults to <strong>cases</strong>.</summary>
    public string GroupName { get; set; } = "cases";

    /// <summary>Specifies a prefix for the API endpoints. Defaults to <i>api</i>.</summary>
    public string PathPrefix { get; set; } = "/api";

    /// <summary>The claim type used to identify the user. Defaults to <i>sub</i>.</summary>
    public string UserClaimType { get; set; } = BasicClaimTypes.Subject;

    /// <summary>The claim type groupid name. Defaults to <strong>group_id</strong></summary>
    public string GroupIdClaimType { get; set; } = "group_id";

    /// <summary>Schema name used for tables. Defaults to <i>case</i>.</summary>
    public string DatabaseSchema { get; set; } = "case";
}
