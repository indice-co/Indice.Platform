using Indice.Events;
using Indice.Features.Cases.Core.Models.Responses;

namespace Indice.Features.Cases.Core.Events.Handlers;

/// <summary>
/// The handler that systemically initiates a new workflow by "CaseType" convention handling the <see cref="CaseSubmittedEvent"/>.
/// <para>The convention is that every Elsa workflow that it is created that needs to be automatically initiated by the system, the <strong>WorkflowDefinition.Tag</strong> must be present and
/// have a valid value that matched the <see cref="CaseType.Code"/> of the application.</para>
/// </summary>
internal class StartWorkflowHandler : IPlatformEventHandler<CaseSubmittedEvent>
{
    public StartWorkflowHandler() {
        
    }

    /// <inheritdoc/>
    public Task Handle(CaseSubmittedEvent @event, PlatformEventArgs args) {
        // 1. Get workflowDefinition
        // 2. If not found exit
        // 3. Create blueprint
        // 4. start workflow
        // 5. Throw if faulted.
        return Task.CompletedTask;
    }
}