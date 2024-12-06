using Indice.Events;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Core.Events.Handlers;

/// <summary>
/// The handler that systemically initiates a new workflow by "CaseType" convention handling the <see cref="CaseSubmittedEvent"/>.
/// <para>The convention is that every Elsa workflow that it is created that needs to be automatically initiated by the system, the <strong>WorkflowDefinition.Tag</strong> must be present and
/// have a valid value that matched the <see cref="CaseType.Code"/> of the application.</para>
/// </summary>
internal class StartWorkflowHandler : IPlatformEventHandler<CaseSubmittedEvent>
{
    public StartWorkflowHandler(ICasesWorkflowManager workflowManager) {
        WorkflowManager = workflowManager;
    }

    public ICasesWorkflowManager WorkflowManager { get; }

    /// <inheritdoc/>
    public async Task Handle(CaseSubmittedEvent @event, PlatformEventArgs args) {
        args.ThrowOnError = true; // notify execution to break everythig.!!! TODO: This is a code smell
        var result = await WorkflowManager.StartWorkflowAsync(@event.Case.Id, @event.CaseTypeCode);
        if (!result.Success) {
            throw new BusinessException(result.Message);
        }
    }
}