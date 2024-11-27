using Indice.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.Core;

/// <summary>Options used to configure the Messages API feature.</summary>
public class MessageEndpointOptions : CampaignOptionsBase
{
    /// <summary>Creates a new instance of <see cref="MessageEndpointOptions"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public MessageEndpointOptions(IServiceCollection services) : base(services) { }

    /// <summary>Creates a new instance of <see cref="MessageEndpointOptions"/>.</summary>
    public MessageEndpointOptions() : base() { }

    /// <summary>The default scope name to be used for Messages API. Defaults to <see cref="MessagesApi.Scope"/>.</summary>
    public string RequiredScope { get; set; } = MessagesApi.Scope;
    /// <summary>Group name for inbox controllers, used in API explorer. If not set, no group is used.</summary>
    public string InboxGroupName { get; set; } = "my";
    /// <summary>Group name for management controllers, used in API explorer. Default is 'messages';</summary>
    public string ManagementGroupName { get; set; } = "messages";
    /// <summary>This is the file upload limit used by the management api when uploading attachements. Defaults to 6 MB</summary>
    public int FileUploadLimit { get; set; } = 6 * 1024 * 1024;
}

/// <summary>Options used to configure the Messages management API feature.</summary>
public class MessageManagementOptions : CampaignOptionsBase
{
    /// <summary>Creates a new instance of <see cref="MessageManagementOptions"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public MessageManagementOptions(IServiceCollection services) : base(services) { }

    /// <summary>Creates a new instance of <see cref="MessageManagementOptions"/>.</summary>
    public MessageManagementOptions() : base() { }

    /// <summary>The default scope name to be used for Messages API. Defaults to <see cref="MessagesApi.Scope"/>.</summary>
    public string RequiredScope { get; set; } = MessagesApi.Scope;
    /// <summary>Group name for management controllers, used in API explorer. Default is 'messages';</summary>
    public string GroupName { get; set; } = "messages";
    /// <summary>This is the file upload limit used by the management api when uploading attachements. Defaults to 6 MB</summary>
    public int FileUploadLimit { get; set; } = 6 * 1024 * 1024;

}

/// <summary>Options used to configure the Campaigns inbox API feature.</summary>
public class MessageInboxOptions : CampaignOptionsBase
{
    /// <summary>Creates a new instance of <see cref="MessageInboxOptions"/>.
    /// </summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public MessageInboxOptions(IServiceCollection services) : base(services) { }

    /// <summary>Creates a new instance of <see cref="MessageInboxOptions"/>.</summary>
    public MessageInboxOptions() : base() { }

    /// <summary>Group name for inbox controllers, used in API explorer. If not set, no group is used.</summary>
    public string GroupName { get; set; } = "my";
}

/// <summary>Base class for Messages API options.</summary>
public class CampaignOptionsBase
{
    /// <summary>Creates a new instance of <see cref="CampaignOptionsBase"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CampaignOptionsBase(IServiceCollection services) {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>Creates a new instance of <see cref="CampaignOptionsBase"/>.</summary>
    public CampaignOptionsBase() { }

    /// <summary>Specifies the contract for a collection of service descriptors.</summary>
    public IServiceCollection Services { get; }
    /// <summary>
    /// Configuration <see cref="Action"/> for internal <see cref="DbContext"/>. 
    /// If not provided the underlying store defaults to SQL Server expecting the setting <i>ConnectionStrings:CampaignsDbConnection</i> to be present.
    /// </summary>
    public Action<IServiceProvider, DbContextOptionsBuilder> ConfigureDbContext { get; set; }
    /// <summary>The claim type used to identify the user. Defaults to <i>sub</i>.</summary>
    public string UserClaimType { get; set; } = BasicClaimTypes.Subject;
    /// <summary>Schema name used for tables. Defaults to <i>cmp</i>.</summary>
    public string DatabaseSchema { get; set; } = MessagesApi.DatabaseSchema;
    /// <summary>Specifies a prefix for the API endpoints.</summary>
    public string PathPrefix { get; set; } = "/";
}
