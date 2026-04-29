using System.Security.Claims;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Identity.Server.SignInLogs;

internal class SignInLogHandlers
{
    public static async Task<Results<Ok<ResultSet<SignInLogEntry>>, ValidationProblem>> GetSignInLogs(
            ISignInLogStore signInLogStore,
            [AsParameters] ListOptions options,
            [AsParameters] SignInLogEntryFilter filter
        ) {
        var signInLogs = await signInLogStore.ListAsync(options, filter);
        return TypedResults.Ok(signInLogs);
    }

    public static async Task<Results<Ok<ResultSet<SignInLogEntry>>, ValidationProblem>> GetMySignInLogs(
            ClaimsPrincipal currentUser,
            ISignInLogStore signInLogStore,
            [AsParameters] ListOptions options,
            [AsParameters] SignInLogEntryFilterBase filter
        ) {
        if (options.Size > 100) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError("size", "Max allowed value for page size is 100."));
        }
        var signInLogs = await signInLogStore.ListAsync(options, new SignInLogEntryFilter {
            From = filter.From,
            To = filter.To,
            ApplicationId = filter.ApplicationId,
            SignInType = filter.SignInType,
            Subject = currentUser.FindSubjectId()
        });
        return TypedResults.Ok(signInLogs);
    }

    public static async Task<Results<NoContent, NotFound>> PatchSignInLog(
            ISignInLogStore signInLogStore,
            Guid rowId,
            SignInLogEntryRequest model
        ) {
        var rowsAffected = await signInLogStore.UpdateAsync(rowId, model);
        return rowsAffected == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    public static async Task<Ok<SignInLogMap>> GetSignInLogMap(
            ISignInLogStore signInLogStore,
            [AsParameters] SignInLogMapFilter filter
        ) {
        var perCountry = await signInLogStore.GetSignInLogMapAsync(filter);
        return TypedResults.Ok(perCountry);
    }
}
