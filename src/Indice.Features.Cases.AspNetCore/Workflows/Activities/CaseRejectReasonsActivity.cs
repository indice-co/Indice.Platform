using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Indice.Features.Cases.Workflows.Activities
{
    [Activity(
        Category = "Cases - Approvals",
        DisplayName = "Reject Reasons",
        Description = "Set the reject reasons for this workflow. This activity works in parallel with the Await Approval Activity.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    internal class CaseRejectReasonsActivity : Activity
    {
        [ActivityInput(
            Label = "RejectReasons",
            Hint = "The reject reasons for the approval component and for the customer notification. The reasons will be stored into global a variable.",
            DefaultSyntax = SyntaxNames.Json,
            UIHint = ActivityInputUIHints.MultiLine,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Json }
        )]
        public IEnumerable<string> RejectReasons { get; set; } = Enumerable.Empty<string>();

        public override ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context) {
            context.SetVariable(CasesApiConstants.WorkflowVariables.RejectReasons, RejectReasons);
            return new ValueTask<IActivityExecutionResult>(new DoneResult());
        }
    }
}