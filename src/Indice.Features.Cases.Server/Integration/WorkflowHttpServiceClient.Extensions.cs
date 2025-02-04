using System.Security.Claims;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Models;
using Indice.Security;
using Microsoft.Extensions.Options;
using CaseSuccessMessage = Indice.Features.Cases.Core.Models.SuccessMessage;

namespace Indice.Features.Cases.Server.Integration;

internal static class WorkflowHttpServiceClient_Extensions
{
    public static Actor ToWorkflowActor(this ClaimsPrincipal user, CasesOptions options) {
        var subject = user.FindFirstValue(BasicClaimTypes.Subject);
        return new Actor {
            UserId = string.IsNullOrWhiteSpace(subject) ? user.FindFirstValue(BasicClaimTypes.ClientId) : subject,
            Reference = user.FindFirstValue(options.ReferenceIdClaimType),
            Email = string.IsNullOrWhiteSpace(subject) ? user.FindFirstValue(BasicClaimTypes.ClientId) : user.FindFirstValue(BasicClaimTypes.Email),
            Name = string.IsNullOrWhiteSpace(subject) ? CasesCoreConstants.SystemUserName : $"{user.FindFirstValue(BasicClaimTypes.GivenName)} {user.FindFirstValue(BasicClaimTypes.FamilyName)}".Trim(),
        };
    }

    public static CustomCaseAction FromHttpCaseActions(this CustomAction action) {
        return new CustomCaseAction {
            Id = action.Id,
            Description = action.Description,
            DefaultValue = action.DefaultValue,
            RedirectToList = action.RedirectToList,
            SuccessMessage = new CaseSuccessMessage {
                Body = action.SuccessMessage?.Body ?? string.Empty,
                Title = action.SuccessMessage?.Title ?? string.Empty
            },
            Class = action.Class,
            HasInput = action.HasInput,
            Label = action.Label,
            Name = action.Name
        };
    }
}

public partial record AvailableActions : IWorkflowActions {}
