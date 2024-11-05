using Indice.Security;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases;

/// <summary>The options for the initialization of the Case Api.</summary>
public abstract class CasesApiOptions
{
    /// <summary>
    /// Configuration <see cref="Action"/> for internal <see cref="DbContext"/>. 
    /// If not provided the underlying store defaults to SQL Server expecting the setting <i>ConnectionStrings:DefaultConnection</i> to be present.
    /// </summary>
    public Action<DbContextOptionsBuilder> ConfigureDbContext { get; set; }
   
    /// <summary>The default scope name to be used for Cases API. Defaults to <see cref="CasesApiConstants.Scope"/>.</summary>
    public string ExpectedScope { get; set; } = CasesApiConstants.Scope;

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

    /// <summary>Enables the Case `ReferenceNumber` feature. Defaults to <see langword="false"/>.</summary>
    public bool ReferenceNumberEnabled { get; set; }
}

/// <summary>
/// The Admin case options, specific for the admin Api.
/// </summary>
public class AdminCasesApiOptions: CasesApiOptions
{

}

/// <summary>
/// The My-Cases options, specific for the my-cases Api.
/// </summary>
public class MyCasesApiOptions: CasesApiOptions
{

}