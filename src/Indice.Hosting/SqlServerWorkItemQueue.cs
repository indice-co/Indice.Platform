using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Indice.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TWorkItem"></typeparam>
    public class SqlServerWorkItemQueue<TWorkItem> : RepositoryBase, IWorkItemQueue<TWorkItem> where TWorkItem : WorkItemBase
    {
        private readonly WorkItemSqlServerOptions _options;
        private readonly ILogger<SqlServerWorkItemQueue<TWorkItem>> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="SqlServerWorkItemQueue{TWorkItem}"/>.
        /// </summary>
        /// <param name="options">Configuration options for the SQL Server table that contains the background jobs.</param>
        /// <param name="connectionFactory"></param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public SqlServerWorkItemQueue(IOptions<WorkItemSqlServerOptions> options, SqlServerConnectionFactory connectionFactory, IConfiguration configuration, ILogger<SqlServerWorkItemQueue<TWorkItem>> logger) : base(connectionFactory) {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task Enqueue(TWorkItem workItem) {
            var columns = typeof(TWorkItem).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(x => x.Name);
            var columnsToInsert = string.Join(", ", columns.Select(x => $"[{x}]"));
            var valuesToInsert = string.Join(", ", columns.Select(x => $"@{x}"));
            var numberOfRowsAffected = await Connection.ExecuteAsync($"INSERT INTO [{_options.SchemaName}].[{_options.TableName}] ({columnsToInsert}) VALUES ({valuesToInsert});", workItem);
            if (numberOfRowsAffected == 1) {
                _logger.LogInformation("Work item '{WorkItem}' was successfully enqueued.");
            }
        }

        /// <inheritdoc />
        public Task<TWorkItem> Dequeue() {
            return Task.FromResult(default(TWorkItem));
        }
    }
}
