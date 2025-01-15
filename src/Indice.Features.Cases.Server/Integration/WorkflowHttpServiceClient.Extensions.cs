using System.Security.Claims;
using Indice.Features.Cases.Core;
using Indice.Security;

namespace Indice.Features.Cases.Server.Integration;

internal static class WorkflowHttpServiceClient_Extensions
{
    public static CasesUser ToCasesUser(this ClaimsPrincipal user, DateTimeOffset? now = null) {
        var subject = user.FindFirstValue(BasicClaimTypes.Subject);
        return new CasesUser {
            Id = string.IsNullOrWhiteSpace(subject) ? user.FindFirstValue(BasicClaimTypes.ClientId) : subject,
            Email = string.IsNullOrWhiteSpace(subject) ? user.FindFirstValue(BasicClaimTypes.ClientId) : user.FindFirstValue(BasicClaimTypes.Email),
            Name = string.IsNullOrWhiteSpace(subject) ? CasesCoreConstants.SystemUserName : $"{user.FindFirstValue(BasicClaimTypes.GivenName)} {user.FindFirstValue(BasicClaimTypes.FamilyName)}".Trim(),
            When = now ?? DateTimeOffset.UtcNow
        };
    }
}