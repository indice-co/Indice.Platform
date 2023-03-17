using Indice.Features.Messages.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.Worker.Azure;

/// <summary>Options used when configuring messages in Azure Functions.</summary>
public class MessageOptions
{
    internal IServiceCollection Services { get; set; }
    /// <summary>
    /// Configuration <see cref="Action"/> for internal <see cref="DbContext"/>. 
    /// If not provided the underlying store defaults to SQL Server expecting the setting <i>ConnectionStrings:CampaignsDbConnection</i> to be present.
    /// </summary>
    public Action<IServiceProvider, DbContextOptionsBuilder> ConfigureDbContext { get; set; }
    /// <summary>Schema name used for tables. Defaults to <i>campaign</i>.</summary>
    public string DatabaseSchema { get; set; } = MessagesApi.DatabaseSchema;
}
