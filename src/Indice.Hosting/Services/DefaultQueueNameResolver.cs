namespace Indice.Hosting.Services;

/// <summary>Resolves the queue name. Uses the configured queue name through the corresponding <see cref="QueueOptions"/>. If that is empty fall-back to the entity name.</summary>
/// <typeparam name="TWorkItem">The type of the work item in the queue</typeparam>
public class DefaultQueueNameResolver<TWorkItem> : IQueueNameResolver<TWorkItem> where TWorkItem : class
{
    private readonly string _queueName;
    /// <summary>Creates a new instance of <see cref="DefaultQueueNameResolver{T}"/>.</summary>
    /// <param name="queueOptions"></param>
    public DefaultQueueNameResolver(QueueOptions queueOptions) {
        _queueName = queueOptions.QueueName;
    }

    /// <summary>Resolves the name of the queue.</summary>
    /// <returns>The name of the queue.</returns>
    public string Resolve(bool isPoison = false) {
        var queueName = _queueName ?? typeof(TWorkItem).Name;
        if (isPoison) {
            queueName += "-poison";
        }
        return queueName;
    }
}
