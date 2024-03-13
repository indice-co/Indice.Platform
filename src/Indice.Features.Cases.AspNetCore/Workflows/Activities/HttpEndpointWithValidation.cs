using Elsa;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Activities.Http.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Esprima.Ast;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Services;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>Add the assignedTo property for a Case.</summary>
[Trigger(Category = "HTTP", DisplayName = "HTTP Endpoint with validation", Description = "Handle an incoming HTTP request and validate schema.", Outcomes = new string[] { OutcomeNames.Done, CasesApiConstants.WorkflowVariables.OutcomeNames.Failed })]
public class HttpEndpointWithValidation : HttpEndpoint
{
    protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) {
        if (Path.Contains("//"))
            throw new Exception("Path cannot contain double slashes (//)");
        return context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternal(context) : Suspend();
    }
    protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => ExecuteInternal(context);
    private readonly ISchemaValidator _schemaValidator = new SchemaValidator();

    private IActivityExecutionResult ExecuteInternal(ActivityExecutionContext context) {
        Output = context.GetInput<HttpRequestModel>()!;
        context.JournalData.Add("Inbound Request", Output);
        if(!_schemaValidator.IsValid(this.Schema, Output.Body)) {
            return Outcome(CasesApiConstants.WorkflowVariables.OutcomeNames.Failed);
        }

        return Done();
    }

}

public class HttpEndpointWithValidationBookmarkkProvider : BookmarkProvider<HttpEndpointBookmark, HttpEndpointWithValidation>
{
    public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<HttpEndpointWithValidation> context, CancellationToken cancellationToken) {
        var path = ToLower((await context.ReadActivityPropertyAsync(x => x.Path, cancellationToken))!);
        var methods = (await context.ReadActivityPropertyAsync(x => x.Methods, cancellationToken))?.Select(ToLower) ?? Enumerable.Empty<string>();

        BookmarkResult CreateBookmark(string method) => Result(new(path, method), nameof(HttpEndpoint));
        return methods.Select(CreateBookmark);
    }

    private static string ToLower(string? s) => s?.ToLowerInvariant();
}