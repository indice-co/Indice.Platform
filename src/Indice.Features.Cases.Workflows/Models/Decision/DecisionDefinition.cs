namespace Indice.Features.Cases.Workflows.Models.Decision;

public class DecisionDefinition
{
    public string Name { get; set; }
    public List<DecisionVariableDefinition> Variables { get; set; }
}