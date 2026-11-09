namespace Indice.Features.Agents.Core.Workflows.State;

/// <summary>
/// Step output for operator workflow steps. It indicates whether the workflow has completed successfully or if it should proceed to the next step.
/// </summary>
/// <param name="Done">Outcome of the workflow.</param>
/// <param name="Requirement">The requirement that needs to be fulfilled. Used for workflow routing.</param>
public record OperationState(bool Done, string? Requirement = null) {
    /// <summary>
    /// Indicates that the workflow has completed successfully.
    /// </summary>
    public static OperationState End => new(Done: true);

    /// <summary>
    /// Indicates that the workflow has completed successfully.
    /// </summary>
    public static OperationState Next(string requirement) => new(Done: false, Requirement: requirement);
};

