using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integrations;
using Indice.Features.Cases.Workflows.Localization;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Cases.Workflows.Activities;

[Activity(
    Category = "Cases - Approvals",
    DisplayName = "Get rejected reason",
    Description = "Get the rejected reason the backofficer has selected. This activity returns a dictionary with translations",
    Outcomes = new[] { OutcomeNames.Done }
)]
internal class GetRejectedReasonActivity(
    ICasesManager casesManager,
    IConfiguration configuration,
    WorkflowSharedResourceService workflowSharedResourceService) : BaseCaseActivity(casesManager)
{
    private readonly string _defaultTranslationLanguage = configuration.GetSection("PrimaryTranslationLanguage").Value ?? CasesWorkflowConstants.DefaultTranslationLanguage;

    [ActivityInput(
        Label = "Select Language",
        Hint = "Select the language to be translated into. If customer's language does not exist, the system's default will be used.",
        Options = new[] { "Customer", "English", "Greek" },
        UIHint = ActivityInputUIHints.RadioList,
        DefaultWorkflowStorageProvider = TransientWorkflowStorageProvider.ProviderName
    )]
    public string? Language { get; set; }

    [ActivityOutput] 
    public string? Output { get; set; }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        var approval = await CasesManager.GetLastApprovalAsync(CaseId!.Value);
        var language = string.Empty;

        switch (Language) {
            case "Customer":
                var @case = await CasesManager.GetByIdAsync(CaseId!.Value, null);
                language = GetCustomerLanguageOrDefault(@case.Metadata?["CurrentCultureName"]);
                break;
            case "English":
                language = "en";
                break;
            case "Greek":
                language = "el";
                break;
        }

        Output = workflowSharedResourceService.GetLocalizedHtmlStringWithCulture(approval?.Reason!, language);
        context.LogOutputProperty(this, nameof(Output), Output);
        return Outcome(OutcomeNames.Done);
    }
    
    private string GetCustomerLanguageOrDefault(string? customerCultureName) {
        if (customerCultureName == null) {
            return _defaultTranslationLanguage;
        }
        return customerCultureName == "en-US" ? "en" : "el";
    }
}