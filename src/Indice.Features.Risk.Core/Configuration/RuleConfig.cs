namespace Indice.Features.Risk.Core.Configuration;

public class RuleConfig
{
    public string RuleName { get; set; } = null!;
    public List<RuleEvent> Events { get; set; } = new List<RuleEvent>();
}
