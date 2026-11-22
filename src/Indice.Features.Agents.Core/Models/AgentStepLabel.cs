namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// The label shown to the user while a workflow step of an agent registered outside this package runs. Register it with
/// <c>services.AddAgentStepLabel(executorId, label)</c>; the built-in steps are labelled by <see cref="AgentMessageLocalizer"/>.
/// </summary>
/// <param name="ExecutorId">The id of the step (executor).</param>
/// <param name="Label">Returns the label in the current UI culture, for example from an <c>IStringLocalizer</c> resolved from the request services.</param>
public sealed record AgentStepLabel(string ExecutorId, Func<IServiceProvider, string> Label);
