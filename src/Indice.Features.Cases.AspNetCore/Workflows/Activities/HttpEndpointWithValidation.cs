using Elsa;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Services;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>Add the assignedTo property for a Case.</summary>
[Trigger(
    Category = "HTTP",
    DisplayName = "HTTP Endpoint with validation",
    Description = "Handle an incoming HTTP request and validate schema.",
    Outcomes = new string[] { OutcomeNames.Done, CasesApiConstants.WorkflowVariables.OutcomeNames.Failed }
)]
public class HttpEndpointWithValidation : HttpEndpoint
{
    /// <inheritdoc/>
    protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) {
        if (Path.Contains("//"))
            throw new Exception("Path cannot contain double slashes (//)");
        return context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternal(context) : Suspend();
    }

    /// <inheritdoc/>
    protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => ExecuteInternal(context);
    
    private readonly ISchemaValidator _schemaValidator = new SchemaValidator();

    private IActivityExecutionResult ExecuteInternal(ActivityExecutionContext context) {
        Output = context.GetInput<HttpRequestModel>()!;
        context.JournalData.Add("Inbound Request", Output);
        if (!_schemaValidator.IsValid(this.Schema, Output.Body)) {
            return Outcome(CasesApiConstants.WorkflowVariables.OutcomeNames.Failed);
        }

        return Done();
    }
}
