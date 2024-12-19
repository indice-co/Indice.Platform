using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Services;
using Indice.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Cases.Core;

/// <summary>Cases core Options and settings</summary>
public class CasesOptions
{
    /// <summary>creates default options</summary>
    public CasesOptions() { }


    /// <summary>creates default options with the ability to register custom services</summary>
    public CasesOptions(IServiceCollection services) { 
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    internal IServiceCollection Services { get; set; } = null!;

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

/// <summary>Options for configuring <see cref="ContactProviderIdentityServer"/>.</summary>
public class ContactProviderIdentityOptions
{
    /// <summary>The base address of the identity system.</summary>
    public Uri? BaseAddress { get; set; }
    /// <summary>The client id used to communicate with Identity Server.</summary>
    public string? ClientId { get; set; }
    /// <summary>The client secret used to communicate with Identity Server.</summary>
    public string? ClientSecret { get; set; }
    /// <summary>The claim type used to identify the user. Defaults to <i>sub</i>.</summary>
    public string UserClaimType { get; set; } = BasicClaimTypes.Subject;
    /// <summary>Indicates that the recipient id is not based on the default claim type used to identify the contact.</summary>
    public bool HasCustomReference => UserClaimType != BasicClaimTypes.Subject;
}
