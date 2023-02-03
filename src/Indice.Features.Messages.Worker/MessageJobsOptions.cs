using Indice.Features.Messages.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.Worker
{
    /// <summary>Options for configuring internal campaign jobs used by the worker host.</summary>
    public class MessageJobsOptions
    {
        internal IServiceCollection Services { get; set; }
        /// <summary>
        /// Configuration <see cref="Action"/> for internal <see cref="DbContext"/>. 
        /// If not provided the underlying store defaults to SQL Server expecting the setting <i>ConnectionStrings:CampaignsDbConnection</i> to be present.
        /// </summary>
        public Action<DbContextOptionsBuilder> ConfigureDbContext { get; set; }
        /// <summary>Schema name used for tables. Defaults to <i>campaign</i>.</summary>
        public string DatabaseSchema { get; set; } = MessagesApi.DatabaseSchema;
        /// <summary>Represents a set of key/value application configuration properties.</summary>
        public IConfiguration Configuration { get; internal set; }
        /// <summary>
        /// Specifies the time interval between two attempts to dequeue new items. Defaults to 300 milliseconds.
        /// </summary>
        public double QueuePollingInterval { get; set; } = 300;
        /// <summary>
        /// Specifies the maximum time interval between two attempts to dequeue new items. Used as a back-off strategy threshold. Defaults to 5000 milliseconds.
        /// </summary>
        public double QueueMaxPollingInterval { get; set; } = 5000;
    }
}
