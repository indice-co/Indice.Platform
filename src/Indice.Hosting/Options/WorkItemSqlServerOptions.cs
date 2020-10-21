namespace Indice.Hosting
{
    /// <summary>
    /// Configuration options for the SQL Server table that contains the background jobs.
    /// </summary>
    public class WorkItemSqlServerOptions
    {
        /// <summary>
        /// The name of the connection string. Default is 'DefaultConnection'.
        /// </summary>
        public string ConnectionStringName { get; set; } = "DefaultConnection";
        /// <summary>
        /// The name of the table that contains the background jobs. Default is 'Jobs'.
        /// </summary>
        public string TableName { get; set; } = "Jobs";
        /// <summary>
        /// The schema of the table that contains the background jobs.  Default is 'dbo'.
        /// </summary>
        public string SchemaName { get; set; } = "dbo";
    }
}
