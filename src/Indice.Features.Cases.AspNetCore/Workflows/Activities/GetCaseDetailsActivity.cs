﻿using System;
using System.Threading.Tasks;
using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases.Workflows.Activities
{
    [Activity(
        Category = "Cases",
        DisplayName = "Get Case Details",
        Description = "Get the details of the case.",
        Outcomes = new[] { OutcomeNames.Done, "Failed" }
    )]
    internal class GetCaseDetailsActivity : BaseCaseActivity
    {
        private readonly IAdminCaseService _adminCaseService;

        public GetCaseDetailsActivity(
            IAdminCaseMessageService caseMessageService,
            IAdminCaseService adminCaseService)
            : base(caseMessageService) {
            _adminCaseService = adminCaseService ?? throw new ArgumentNullException(nameof(adminCaseService));
        }

        [ActivityOutput]
        public object Output { get; set; }

        public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
            CaseId ??= Guid.Parse(context.CorrelationId);
            // Run as systemic user, since this is a system activity for creating conditions at workflow
            var systemUser = Cases.Extensions.PrincipalExtensions.SystemUser();
            var @case = await _adminCaseService.GetCaseById(systemUser, CaseId.Value!);
            Output = @case;
            context.LogOutputProperty(this, nameof(Output), Output);
            return Done(Output);
        }
    }
}