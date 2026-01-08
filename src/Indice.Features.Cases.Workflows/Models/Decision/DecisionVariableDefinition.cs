namespace Indice.Features.Cases.Workflows.Models.Decision;

public class DecisionVariableDefinition
{
    public string Name { get; set; } = default!;
    public DecisionVariableType Type { get; set; }
    
    public IEnumerable<string>? AllowedValues { get; set; }
}

public enum DecisionVariableType
{
    Int,
    String,
    Bool,
    Date
}