using System.Text;
using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Workflows.Extensions;
using Indice.Services;

namespace Indice.Features.Cases.Workflows.Activities
{
    [Activity(
        Category = "Cases",
        DisplayName = "Notify customer",
        Description = "Notify the customer via sms/viber, PushNotification or Email, with a specific message regarding the status of the case.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    internal class NotifyCustomerActivity : BaseCaseActivity
    {
        private readonly IAdminCaseService _adminCaseService;
        private readonly ISmsService _smsService;

        public NotifyCustomerActivity(
            IAdminCaseMessageService caseMessageService,
            IAdminCaseService adminCaseService,
            ISmsService smsService)
            : base(caseMessageService) {
            _adminCaseService = adminCaseService ?? throw new ArgumentNullException(nameof(adminCaseService));
            _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        }
        
        [ActivityInput(
            Label = "Subject - EN",
            Hint = "The subject of the notification",
            DefaultSyntax = SyntaxNames.JavaScript,
            UIHint = ActivityInputUIHints.MultiLine,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Literal }
        )]
        public string SubjectEN { get; set; } = string.Empty;
        [ActivityInput(
            Label = "Subject - EL",
            Hint = "The subject of the notification",
            DefaultSyntax = SyntaxNames.JavaScript,
            UIHint = ActivityInputUIHints.MultiLine,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Literal }
        )]
        public string SubjectEL { get; set; } = string.Empty;
        [ActivityInput(
            Label = "Body - EN",
            Hint = "The body of the notification.",
            DefaultSyntax = SyntaxNames.Literal,
            UIHint = ActivityInputUIHints.MultiLine,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Literal }
        )]
        public string BodyEN { get; set; } = string.Empty;
        [ActivityInput(
           Label = "Body - EL",
           Hint = "The body of the notification.",
           DefaultSyntax = SyntaxNames.Literal,
           UIHint = ActivityInputUIHints.MultiLine,
           SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Literal }
       )]
        public string BodyEL { get; set; } = string.Empty;
        [ActivityInput(
            Label = "Delivery channel",
            Hint = "The notification channel to send the message.",
            Options = new[] { "SMS/Viber" /*"PushNotification", "Email"*/ },
            UIHint = ActivityInputUIHints.CheckList,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript },
            DefaultWorkflowStorageProvider = TransientWorkflowStorageProvider.ProviderName
        )]
        public IEnumerable<string> DeliveryChannel { get; set; } = new List<string>();

        [ActivityOutput]
        public string InfoMessage { get; set; }

        public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
            if (!DeliveryChannel.Any() || (string.IsNullOrEmpty(BodyEL) && string.IsNullOrEmpty(BodyEL))) {
                context.LogOutputProperty(this, nameof(InfoMessage), "Delivery Channel or notification body not provided.");
                return Outcome(OutcomeNames.Done);
            }

            var @case = await _adminCaseService.GetCaseById(context.TryGetUser()!, CaseId!.Value);
            var infoMessage = new StringBuilder();
            var subject = default(string);
            var body = default(string);
            foreach (var channel in DeliveryChannel) {
                switch (channel) {
                    case "SMS/Viber": {
                            var customerPhoneNumber = @case.Metadata?["CustomerPhoneNumber"]; // this can be activity input to match different cases (eg CustomerPhoneNumber or PhoneNumber)
                            if (string.IsNullOrEmpty(customerPhoneNumber)) {
                                infoMessage.Append("Customer phone number is empty in case metadata. Notification will not be send.");
                                continue;
                            }
                            infoMessage.Append($"Customer has been notified through \"{channel}\".");
                            var lang = @case.Metadata?["CurrentCultureName"]; // el-GR, en-US, en-GB
                            subject = lang == "el-GR" ? SubjectEL : SubjectEN;
                            body = lang == "el-GR" ? BodyEL : BodyEN;
                            await _smsService.SendAsync(customerPhoneNumber, subject, body);
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(channel), channel, "Delivery channel not found.");
                }
            }

            context.LogOutputProperty(this, nameof(InfoMessage), infoMessage.ToString());
            return Outcome(OutcomeNames.Done, new { subject, body });
        }
    }
}