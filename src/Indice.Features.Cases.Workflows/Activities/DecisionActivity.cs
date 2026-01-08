using System.Dynamic;
using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Extensions;
using Indice.Features.Cases.Workflows.Integrations;
using Indice.Features.Cases.Workflows.Models.Decision;
using Indice.Features.Cases.Workflows.Store;
using RulesEngine.Models;
using CustomOutcomeNames = Indice.Features.Cases.Workflows.CasesWorkflowConstants.WorkflowVariables.OutcomeNames;

namespace Indice.Features.Cases.Workflows.Activities;

[Activity(
    Category = "Cases",
    DisplayName = "Decision Activity",
    Description = "Execute the decision rules and provide the result as Activity Output.",
    Outcomes = new[] { OutcomeNames.Done, CustomOutcomeNames.Failed }
)]
internal class DecisionActivity(ICasesManager casesManager, CasesManagerHttpClient client, DecisionStore store) : BaseCaseActivity(casesManager)
{
    [ActivityInput(
        Label = "The name of the Decision"
    )]
    public string DecisionName { get; set; }
    
    [ActivityInput(
        Label = "Enter one or more rule variables names.",
        Hint = "The variables upon which the decision will be made",
        UIHint = ActivityInputUIHints.MultiText,
        DefaultSyntax = SyntaxNames.Json,
        SupportedSyntaxes = [SyntaxNames.Json],
        IsDesignerCritical = true
    )]
    public IEnumerable<DecisionVariableDefinition> DecisionVariables { get; set; } = new List<DecisionVariableDefinition>();
    
    [ActivityInput(
        Label = "DecisionInput",
        Hint = "The decision inputs.",
        SupportedSyntaxes = [SyntaxNames.JavaScript],
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultWorkflowStorageProvider = TransientWorkflowStorageProvider.ProviderName
    )]
    public ExpandoObject DecisionInput { get; set; } = null!;
    
    [ActivityOutput]
    public object? Output { get; set; }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);

        var workflow = await store.GetDecision(context.WorkflowExecutionContext.WorkflowBlueprint.Tag, DecisionName);
        // var decisionTable = await store.GetDecisionTable(context.WorkflowExecutionContext.WorkflowBlueprint.Tag, DecisionName);
        var rulesEngine = new RulesEngine.RulesEngine([workflow]);
        
        List<RuleResultTree> resultList = await rulesEngine.ExecuteAllRulesAsync(workflow.WorkflowName, DecisionInput);
        
        var successfulRuleResult = resultList.FirstOrDefault(ruleResult => ruleResult.IsSuccess);
        var defaultValue = "Rule Execution Failed. No rule matches the input case";
        // var rr = decisionTable?.HitPolicy switch {
        //     HitPolicy.All => resultList.Where(r => r.IsSuccess).SelectMany(r => r.Rule.SuccessEvent),
        //     HitPolicy.First => resultList.FirstOrDefault(r => r.IsSuccess)?.Rule.SuccessEvent ?? defaultValue,
        //     HitPolicy.Priority => throw new InvalidOperationException(),
        //     HitPolicy.Unique => resultList.Count != 1 ? throw new InvalidOperationException() : resultList.FirstOrDefault(r => r.IsSuccess).Rule.SuccessEvent
        //     _ => throw new ArgumentOutOfRangeException()
        // };
        var result = successfulRuleResult is not null ? 
            successfulRuleResult.Rule.SuccessEvent :
            defaultValue;

        Output = result;
        context.LogOutputProperty(this, "Output", Output);
        await CasesManager.Send(CaseId!.Value, context.TryGetLastActor(), new Message { Comment = $"Decision Outcome: {result}", PrivateComment = true});
        return Outcome(result);
    }
}
