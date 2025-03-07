using Elsa;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>Add the assignedTo property for a Case.</summary>
[Trigger(
    Category = "HTTP",
    DisplayName = "HTTP Endpoint with validation",
    Description = "Handle an incoming HTTP request and validate schema.",
    Outcomes = new string[] { OutcomeNames.Done, CasesWorkflowConstants.WorkflowVariables.OutcomeNames.Failed }
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
    
    private IActivityExecutionResult ExecuteInternal(ActivityExecutionContext context) {
        Output = context.GetInput<HttpRequestModel>()!;
        context.JournalData.Add("Inbound Request", Output);

        //Skip validation when there is no Schema
        if (Schema is null) {
            return Done();
        }
        //There is schema by the body is null
        else if (Output.Body is null) {
            return Outcome(CasesWorkflowConstants.WorkflowVariables.OutcomeNames.Failed);
        }
        //Validate body with schema
        var schemaValidator = context.ServiceProvider.GetRequiredService<ISchemaValidator>();
        if (!schemaValidator.IsValid(Schema, Output.Body)) {
            return Outcome(CasesWorkflowConstants.WorkflowVariables.OutcomeNames.Failed);
        }
        return Done();
    }
}
