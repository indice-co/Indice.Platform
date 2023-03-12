using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Events;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Workflows.Specifications;

namespace Indice.Features.Cases.Handlers;

/// <summary>
/// The handler that systemically initiates a new workflow by "CaseType" convention handling the <see cref="CaseSubmittedEvent"/>.
/// <para>The convention is that every Elsa workflow that it is created that needs to be automatically initiated by the system, the <see cref="WorkflowDefinition.Tag"/> must be present and
/// have a valid value that matched the <see cref="DbCaseType.Code"/> of the application.</para>
/// </summary>
internal class StartWorkflowHandler : ICaseEventHandler<CaseSubmittedEvent>
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IStartsWorkflow _startsWorkflow;
    private readonly IWorkflowBlueprintMaterializer _workflowBlueprintMaterializer;

    public StartWorkflowHandler(
        IWorkflowDefinitionStore workflowDefinitionStore,
        IStartsWorkflow startsWorkflow,
        IWorkflowBlueprintMaterializer workflowBlueprintMaterializer) {
        _workflowDefinitionStore = workflowDefinitionStore ?? throw new ArgumentNullException(nameof(workflowDefinitionStore));
        _startsWorkflow = startsWorkflow ?? throw new ArgumentNullException(nameof(startsWorkflow));
        _workflowBlueprintMaterializer = workflowBlueprintMaterializer ?? throw new ArgumentNullException(nameof(workflowBlueprintMaterializer));
    }

    public async Task Handle(CaseSubmittedEvent @event) {
        var workflowDefinitionTagSpecification = new WorkflowDefinitionTagCsvSpecification(@event.CaseTypeCode);
        var workflowDefinition = await _workflowDefinitionStore.FindAsync(workflowDefinitionTagSpecification);
        if (workflowDefinition == null) {
            return;
        }

        var blueprint = await _workflowBlueprintMaterializer.CreateWorkflowBlueprintAsync(workflowDefinition);
        var instance = await _startsWorkflow.StartWorkflowAsync(
            blueprint,
            null,
            new WorkflowInput(@event.Case.Id),
            @event.Case.Id.ToString());

        if (instance.WorkflowInstance?.Faults is { Count: > 0 }) {
            throw new Exception(instance.WorkflowInstance?.Faults.FirstOrDefault()?.Message);
        }
    }
}