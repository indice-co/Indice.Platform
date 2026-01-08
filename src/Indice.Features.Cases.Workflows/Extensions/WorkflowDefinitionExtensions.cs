using Elsa.Models;

namespace Indice.Features.Cases.Workflows.Extensions;

public static class WorkflowDefinitionExtensions
{
    public static string? GetProperty(this ActivityDefinition activity, string propertyName) {
        return activity.Properties.First(x => x.Name == propertyName).Expressions.First().Value;
    }
}