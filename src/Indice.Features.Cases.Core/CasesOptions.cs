using Indice.Features.Cases.Core.Data;
using Indice.Security;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Core;

/// <summary>Cases core Options and settings</summary>
public class CasesOptions
{
    /// <summary>The default scope name to be used for Cases API. Defaults to <strong>cases</strong>.</summary>
    public string RequiredScope { get; set; } = CasesCoreConstants.DefaultScopeName;

    /// <summary>Enables the Case `ReferenceNumber` feature. Defaults to <see langword="false"/>.</summary>
    public bool ReferenceNumberEnabled { get; set; }
    /// <summary>The claim type groupid name</summary>
    public string GroupIdClaimType { get; set; } = CasesCoreConstants.DefaultGroupIdClaimType;

    /// <summary>The claim type used to identify the user. Defaults to <i>sub</i>.</summary>
    public string UserClaimType { get; set; } = BasicClaimTypes.Subject;

    /// <summary>Schema name used for tables. Defaults to <i>case</i>.</summary>
    public string DatabaseSchema { get; set; } = CasesCoreConstants.DatabaseSchema;

    /// <summary>
    /// Configuration <see cref="Action"/> for internal <see cref="DbContext"/>. 
    /// If not provided the underlying store defaults to SQL Server expecting the setting <i>ConnectionStrings:DefaultConnection</i> to be present.
    /// </summary>
    public Action<IServiceProvider, DbContextOptionsBuilder>? ConfigureDbContext { get; set; }


    /// <summary>
    /// Configuration <see cref="Action"/> for internal seed of the <see cref="CasesDbContext"/>. 
    /// </summary>
    public Action<CasesDbIntialDataOptions>? ConfigureDbSeed { get; set; }
}
