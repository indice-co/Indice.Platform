using System.Security.Claims;
using Azure.Storage.Queues;
using Indice.Services;

namespace Indice.Features.Messages.Core
{
    /// <summary>Options for configuring <see cref="EventDispatcherAzure"/>.</summary>
    public class MessagesEventDispatcherAzureOptions
    {
        /// <summary>The connection string to the Azure Storage account. By default it searches for <see cref="EventDispatcherAzure.CONNECTION_STRING_NAME"/> application setting inside ConnectionStrings section.</summary>
        public string ConnectionString { get; set; }
        /// <summary>The environment name to use. Defaults to 'Production'.</summary>
        public string EnvironmentName { get; set; } = "Production";
        /// <summary>Provides a way to enable/disable event dispatching at will. Defaults to true.</summary>
        public bool Enabled { get; set; } = true;
        /// <summary>A function that retrieves the current thread user from the current operation context.</summary>
        public Func<ClaimsPrincipal> ClaimsPrincipalSelector { get; set; }
        /// <summary>A function that retrieves the current tenant id by any means possible. This is optional.</summary>
        public Func<Guid?> TenantIdSelector { get; set; }
        /// <summary>Determines how <see cref="Azure.Storage.Queues.Models.QueueMessage.Body"/> is represented in HTTP requests and responses.</summary>
        public QueueMessageEncoding QueueMessageEncoding { get; set; } = QueueMessageEncoding.Base64;
    }
}
