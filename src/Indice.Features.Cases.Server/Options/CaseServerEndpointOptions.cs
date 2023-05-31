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

    /// <summary>The default scope name to be used for Cases API. Defaults to <see cref="CasesApiConstants.Scope"/>.</summary>
    public string ApiScope { get; set; } = CasesApiConstants.Scope;

    /// <summary>Cases GroupName Api Explorer. Defaults to <see cref="CasesApiConstants.GroupName"/>.</summary>
    public string GroupName { get; set; } = CasesApiConstants.GroupName;

    /// <summary>Specifies a prefix for the API endpoints. Defaults to <i>api</i>.</summary>
    public string ApiPrefix { get; set; } = "api";

    /// <summary>The claim type used to identify the user. Defaults to <i>sub</i>.</summary>
    public string UserClaimType { get; set; } = BasicClaimTypes.Subject;

    /// <summary>Schema name used for tables. Defaults to <i>case</i>.</summary>
    public string DatabaseSchema { get; set; } = CasesApiConstants.DatabaseSchema;

    /// <summary>The claim type groupid name</summary>
    public string GroupIdClaimType { get; set; } = CasesApiConstants.DefaultGroupIdClaimType;
}
