namespace Indice.Features.Risk.Core.Configuration;

public class RuleConfig
{
    public string RuleName { get; set; } = null!;
    public List<RiskEventModel> Events { get; set; } = new List<RiskEventModel>();
}
