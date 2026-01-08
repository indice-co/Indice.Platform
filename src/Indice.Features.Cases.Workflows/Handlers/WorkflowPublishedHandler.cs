using Elsa.Events;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Store;
using MediatR;

namespace Indice.Features.Cases.Workflows.Handlers;

public class WorkflowPublishedHandler(DecisionStore store) : INotificationHandler<WorkflowDefinitionPublished>
{
    public async Task Handle(WorkflowDefinitionPublished notification, CancellationToken cancellationToken) {
        var definition = notification.WorkflowDefinition;

        var decisionActivities = definition.Activities.Where(a => a.Type == nameof(DecisionActivity));
        foreach (var activity in decisionActivities) {
            var decisionName = activity.Properties.First(x => x.Name == "DecisionName").Expressions.First().Value;
            await store.CreateDecision(definition.Tag!, decisionName!);
        }
    }
}