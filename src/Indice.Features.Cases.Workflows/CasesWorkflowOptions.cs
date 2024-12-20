using System.Reflection;
using Elsa.Activities.Email;
using Elsa.Activities.Email.Options;
using Elsa.Retention.Contracts;
using Elsa.Retention.Options;
using Elsa.Retention.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Cases.Workflows;

/// <summary>Cases workflow Options and settings</summary>
public class CasesWorkflowOptions
{
    /// <summary>creates default options</summary>
    public CasesWorkflowOptions() { }


    /// <summary>creates default options with the ability to register custom services</summary>
    public CasesWorkflowOptions(IServiceCollection services) {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    internal IServiceCollection Services { get; set; } = null!;

    /// <summary>
    /// Configuration <see cref="Action"/> for internal <see cref="DbContext"/>. 
    /// If not provided the underlying store defaults to SQL Server expecting the setting <i>ConnectionStrings:DefaultConnection</i> to be present.
    /// </summary>
    public Action<IServiceProvider, DbContextOptionsBuilder>? ConfigureDbContext { get; set; }

    /// <summary>
    /// Configure SMTP options for the <see cref="SendEmail"/> activity.
    /// </summary>
    public Action<SmtpOptions>? ConfigureSmtp { get; set; }
    /// <summary>
    /// Configure RetentionServices options.
    /// </summary>
    public Action<CleanupOptions>? ConfigureRetentionServices { get; set; }

    /// <summary>Controls the registration of the retention services</summary>
    public bool RetentionServicesEnabled { get; set; } = true;

    /// <summary>
    /// The assembly with the workflow activities and definitions to register.
    /// </summary>
    public Func<Assembly>? GetWorkflowAssembly { get; set; }

    /// <summary>
    /// Override the specification filter that will select the workflows for deletion. If the value is null the default <see cref="CompletedWorkflowFilterSpecification"/> will be used.
    /// </summary>
    public IRetentionSpecificationFilter? RetentionSpecificationFilter { get; set; }

    /// <summary>Elsa server base path</summary>
    public string? ServerBasePath { get; set; }
    /// <summary>Elsa server base Uri</summary>
    public string? ServerBaseUrl { get; set; }
}
