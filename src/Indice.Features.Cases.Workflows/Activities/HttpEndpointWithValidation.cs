using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Extensions;
using Indice.Features.Cases.Workflows.Models;
using Indice.Serialization;
using Json.More;
using Json.Schema;
using CustomOutcomeNames = Indice.Features.Cases.Workflows.CasesWorkflowConstants.WorkflowVariables.OutcomeNames;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>Add the assignedTo property for a Case.</summary>
[Trigger(
    Category = "HTTP",
    DisplayName = "HTTP Endpoint with validation",
    Description = "Handle an incoming HTTP request and validate schema.",
    Outcomes = new[] { OutcomeNames.Done, CustomOutcomeNames.Failed }
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
        context.TrySetLastActor();

        // Skip validation when there is no Schema
        if (Schema is null) {
            return Done();
        }
        
        // There is schema but the request body is null
        if (Output.Body is null) {
            return Outcome(CustomOutcomeNames.Failed);
        }
        
        // Validate body with schema
        if (!ValidateJsonSchema(Schema, Output.Body)) {
            return Outcome(CasesWorkflowConstants.WorkflowVariables.OutcomeNames.Failed);
        }
        
        return Done();
    }

    private bool ValidateJsonSchema(string schema, object data) {
        ArgumentException.ThrowIfNullOrEmpty(schema);
        ArgumentNullException.ThrowIfNull(data);
        var mySchema = JsonSchema.FromText(schema);
        var jsonNode = (data, data.GetType().Name) switch {
            (JsonElement element, _) => element.AsNode(),
            (JsonNode node, _) => node,
            (object jObject, "JObject") => JsonNode.Parse(jObject.ToString()!), // this is smarter because we do not require a reference to the Newtonsoft library
            (string text, _) => JsonNode.Parse(text),
            _ => JsonNode.Parse(JsonSerializer.Serialize(data, JsonSerializerOptionDefaults.GetDefaultSettings()))
        };

        var validate = mySchema.Evaluate(jsonNode.ToJsonDocument().RootElement, new EvaluationOptions {
            OutputFormat = OutputFormat.List
        });

        return validate.IsValid;
    }
}
