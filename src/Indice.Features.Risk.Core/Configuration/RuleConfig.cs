namespace Indice.Features.Risk.Core.Configuration;

internal class RuleConfig
{
    public string RuleName { get; set; } = null!;
    public List<RuleEvent> Events { get; set; } = new List<RuleEvent>();
}
