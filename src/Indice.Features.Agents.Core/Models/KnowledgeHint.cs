namespace Indice.Features.Agents.Core.Models;

/// <summary>A knowledge base category that matches a message, and the agent(s) that own that knowledge.</summary>
/// <param name="Category">The category, as an agent declares it in <see cref="AgentInfo.KnowledgeCategories"/>.</param>
/// <param name="Score">The best similarity of any chunk of the category to the message, from 0 to 1.</param>
/// <param name="AgentNames">The agents, among those the caller may use, that declare the category.</param>
public sealed record KnowledgeHint(string Category, double Score, IReadOnlyList<string> AgentNames);
