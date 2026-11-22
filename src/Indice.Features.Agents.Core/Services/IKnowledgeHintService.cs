using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Tells the master router which agent's knowledge a message is about, by matching it against the knowledge base. This is how
/// an agent that declares <see cref="AgentInfo.KnowledgeCategories"/> is picked when the context fits.
/// </summary>
public interface IKnowledgeHintService
{
    /// <summary>Finds the categories, among those declared by <paramref name="allowedAgents"/>, that best match <paramref name="message"/>.</summary>
    /// <param name="message">The latest user message.</param>
    /// <param name="allowedAgents">The agents the caller may use. Categories of any other agent are never searched for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching categories, best first. Empty when hints are off, no agent declares a category, or nothing matches.</returns>
    Task<IReadOnlyList<KnowledgeHint>> GetHintsAsync(string message, IReadOnlyList<AgentInfo> allowedAgents, CancellationToken cancellationToken = default);
}
