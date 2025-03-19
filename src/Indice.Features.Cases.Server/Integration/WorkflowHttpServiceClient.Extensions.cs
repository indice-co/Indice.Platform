using System.Globalization;
using System.Security.Claims;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Models;
using Indice.Security;
using Microsoft.Extensions.Options;
using CaseSuccessMessage = Indice.Features.Cases.Core.Models.SuccessMessage;

namespace Indice.Features.Cases.Server.Integration;

/// <summary>Implement <see cref="IWorkflowActions"/></summary>
public partial record AvailableActions : IWorkflowActions {}


internal static class WorkflowHttpServiceClient_Extensions
{
    /// <summary>Creates a http <see cref="Actor"/> model from the current user.</summary>
    public static Actor ToActor(this ClaimsPrincipal user, CasesOptions options) {
        var subject = user.FindFirstValue(BasicClaimTypes.Subject);
        return new Actor {
            Id = string.IsNullOrWhiteSpace(subject) ? user.FindFirstValue(BasicClaimTypes.ClientId) : subject,
            Reference = user.FindFirstValue(options.ReferenceIdClaimType),
            GroupId = user.FindFirstValue(options.GroupIdClaimType),
            Email = string.IsNullOrWhiteSpace(subject) ? user.FindFirstValue(BasicClaimTypes.ClientId) : user.FindFirstValue(BasicClaimTypes.Email),
            Name = string.IsNullOrWhiteSpace(subject) ? CasesCoreConstants.SystemUserName : $"{user.FindFirstValue(BasicClaimTypes.GivenName)} {user.FindFirstValue(BasicClaimTypes.FamilyName)}".Trim(),
            CurrentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };
    }

    /// <summary>Simple mapping from Cases <see cref="WorkflowActor"/> to http <see cref="Actor"/></summary>
    public static Actor ToActor(this WorkflowActor actor) {
        return new Actor {
            Id = actor.Id,
            Reference = actor.Reference,
            GroupId = actor.GroupId,
            Email = actor.Email,
            Name = actor.Name,
            CurrentCulture = actor.CurrentCulture
        };
    }

    /// <summary>Create <see cref="CustomCaseAction"/> from workflow <see cref="CustomAction"/></summary>
    public static CustomCaseAction CreateFromWorkflowAction(this CustomAction action) {
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