namespace Indice.Hosting.Services;

/// <summary>Resolves the queue name.</summary>
/// <typeparam name="TWorkItem">The type of the work item in the queue</typeparam>
public interface IQueueNameResolver<TWorkItem> where TWorkItem : class
{
    /// <summary>Resolves the name of the queue.</summary>
    /// <returns>The name of the queue.</returns>
    string Resolve(bool isPoison = false);
}
