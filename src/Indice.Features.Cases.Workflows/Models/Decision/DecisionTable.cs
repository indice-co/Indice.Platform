namespace Indice.Features.Cases.Workflows.Models.Decision;

public class DecisionTable
{
    public required string DecisionName { get; set; }
    
    public HitPolicy HitPolicy { get; set; }

    public List<DecisionRule> Rules { get; set; } = [];
}

public class DecisionRule
{
    public string RuleName { get; set; } = null!;

    public List<RuleCondition> Conditions { get; set; } = [];

    public string SuccessEvent { get; set; } = null!;

    public string? ErrorMessage { get; set; }
}

public class RuleCondition
{
    public required string Field { get; set; }
    
    public required FieldType FieldType { get; set; }

    public required string Operator { get; set; }

    public required string? Value { get; set; }
}

public enum FieldType
{
    Int,
    String,
    Bool,
    Date
}

public enum HitPolicy
{
    Unique,
    All,
    First,
    Priority,
    Collect
}