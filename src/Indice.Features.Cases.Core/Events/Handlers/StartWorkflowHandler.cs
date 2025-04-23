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
    public ICasesWorkflowManager WorkflowManager { get; }
    public IPlatformEventService PlatformEventService { get; }
    
    public StartWorkflowHandler(ICasesWorkflowManager workflowManager, IPlatformEventService platformEventService) {
        WorkflowManager = workflowManager;
        PlatformEventService = platformEventService;
    }

    /// <inheritdoc/>
    public async Task Handle(CaseSubmittedEvent @event, PlatformEventArgs args) {
        var result = await WorkflowManager.StartWorkflowAsync(@event.Case.Id, @event.CaseTypeCode, @event.WorkflowActor);
        if (!result.Success) {
            await PlatformEventService.Publish(new StartWorkflowFaultedEvent(@event.Case, @event.CaseTypeCode, @event.WorkflowActor, result.Message));
            throw new BusinessException(result.Message);
        }
    }
}