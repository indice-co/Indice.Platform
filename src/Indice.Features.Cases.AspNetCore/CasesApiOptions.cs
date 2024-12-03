using Indice.AspNetCore.Configuration;
using Indice.Features.Cases.Core;
using Indice.Security;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases;

/// <summary>The options for the initialization of the Case Api.</summary>
public abstract class CasesApiOptions : CasesOptions
{
    /// <summary>
    /// Configuration for overriding the default <see cref="LimitUploadOptions"/>
    /// </summary>
    public Action<LimitUploadOptions>? ConfigureLimitUpload { get; set; }
   
    /// <summary>Cases GroupName Api Explorer. Defaults to <see cref="CasesApiConstants.GroupName"/>.</summary>
    public string GroupName { get; set; } = CasesApiConstants.GroupName;
    
    /// <summary>Specifies a prefix for the API endpoints. Defaults to <i>api</i>.</summary>
    public string ApiPrefix { get; set; } = "api";
}

/// <summary>
/// The Admin case options, specific for the admin Api.
/// </summary>
public class AdminCasesApiOptions : CasesApiOptions
{

}

/// <summary>
/// The My-Cases options, specific for the my-cases Api.
/// </summary>
public class MyCasesApiOptions: CasesApiOptions
{

}