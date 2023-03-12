using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Resources;
using Indice.Features.Cases.Workflows.Extensions;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Cases.Workflows.Activities;

[Activity(
    Category = "Cases - Approvals",
    DisplayName = "Get rejected reason",
    Description =
        "Get the rejected reason the backofficer has selected. This activity returns a dictionary with translations",
    Outcomes = new[] { OutcomeNames.Done }
)]
internal class GetRejectedReasonActivity : BaseCaseActivity
{
    private readonly CaseSharedResourceService _caseSharedResourceService;
    private readonly IAdminCaseService _adminCaseService;
    private readonly ICaseApprovalService _caseApprovalService;
    private readonly string _defaultTranslationLanguage;

    public GetRejectedReasonActivity(
        IAdminCaseMessageService caseMessageService,
        CaseSharedResourceService caseSharedResourceService,
        IAdminCaseService adminCaseService,
        ICaseApprovalService caseApprovalService,
        IConfiguration configuration)
        : base(caseMessageService) {
        _caseSharedResourceService = caseSharedResourceService ?? throw new ArgumentNullException(nameof(caseSharedResourceService));
        _adminCaseService = adminCaseService ?? throw new ArgumentNullException(nameof(adminCaseService));
        _caseApprovalService = caseApprovalService ?? throw new ArgumentNullException(nameof(caseApprovalService));
        _defaultTranslationLanguage = configuration.GetSection("PrimaryTranslationLanguage").Value ?? CasesApiConstants.DefaultTranslationLanguage;
    }

    [ActivityInput(
        Label = "Select Language",
        Hint = "Select the language to be translated into. If customer's language does not exist, the system's default will be used.",
        Options = new[] { "Customer", "English", "Greek" },
        UIHint = ActivityInputUIHints.RadioList,
        DefaultWorkflowStorageProvider = TransientWorkflowStorageProvider.ProviderName
    )]
    public string Language { get; set; }

    [ActivityOutput] 
    public string Output { get; set; }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        var approval = await _caseApprovalService.GetLastApproval(CaseId!.Value);
        var language = string.Empty;

        switch (Language) {
            case "Customer":
                var @case = await _adminCaseService.GetCaseById(context.TryGetUser()!, CaseId!.Value);
                language = GetCustomerLanguageOrDefault(@case.Metadata?["CurrentCultureName"]);
                break;
            case "English":
                language = "en";
                break;
            case "Greek":
                language = "el";
                break;
        }

        Output = _caseSharedResourceService.GetLocalizedHtmlStringWithCulture(approval.Reason, language);
        context.LogOutputProperty(this, nameof(Output), Output);
        return Outcome(OutcomeNames.Done);
    }
    
    private string GetCustomerLanguageOrDefault(string customerCultureName) {
        if (customerCultureName == null) {
            return _defaultTranslationLanguage;
        }
        return customerCultureName == "en-US" ? "en" : "el";
    }
}