using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

internal static class ExternalIntegrationHandler
{


    public static async Task<Results<ValidationProblem, NoContent, NotFound, ProblemHttpResult>> SendExternalMessage(
        [FromRoute] Guid caseId,
        [FromRoute] string? endpointName,
        [FromBody] JsonNode request,
        [FromServices] IElsaClient casesService,
        ClaimsPrincipal user,
        IOptions<CasesOptions> caseOptions
    ) {
        if (request is not JsonObject) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError("", "The request body must be a JSON object."));
        }
        request["actor"] = user.UserToActor(caseOptions.Value).ToJsonNode();
        var (statusCode, error) = await SendMessage(casesService, caseId, endpointName, request);
        if (statusCode != StatusCodes.Status200OK) {
            if (statusCode == StatusCodes.Status404NotFound) {
                return TypedResults.NotFound();
            }
            if (statusCode == StatusCodes.Status400BadRequest) {
                return TypedResults.ValidationProblem(ValidationErrors.AddError("", error ?? string.Empty));
            }
            if (statusCode == StatusCodes.Status409Conflict) {
                return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, detail: error ?? string.Empty);
            }
            return TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: error ?? "critical error");
        }
        return TypedResults.NoContent();
    }

    private static async Task<(int, string)> SendMessage(IElsaClient casesClient, Guid caseId, string? endpointName, object request) {
        try {
            await casesClient.SendExternalMessage(caseId, endpointName, request);
        } catch (ElsaClient.CasesClientException<ValidationProblemDetails> ex) {
            if (ex.StatusCode == StatusCodes.Status409Conflict) {
                var errMsg = ex.Result.Extensions.FirstOrDefault(static f => string.Equals(f.Key, "message", StringComparison.OrdinalIgnoreCase)).Value?.ToString() ?? string.Empty;
                return (StatusCodes.Status409Conflict, errMsg);
            }

            throw;
        } catch (ElsaClient.CasesClientException<ProblemDetails> ex) {
            if (ex.StatusCode == StatusCodes.Status404NotFound) {
                return (StatusCodes.Status404NotFound, string.Empty);
            }
            if (ex.StatusCode == StatusCodes.Status400BadRequest) {
                return (StatusCodes.Status400BadRequest, ex.Result!.Extensions!.ContainsKey("message")
                    ? ex.Result!.Extensions["message"]?.ToString()!
                    : string.Empty);
            }
            throw;
        } catch (ElsaClient.CasesClientException ex) {
            if (ex.StatusCode == StatusCodes.Status404NotFound) {
                return (StatusCodes.Status404NotFound, string.Empty);
            }
            if (ex.StatusCode == StatusCodes.Status400BadRequest) {
                return (StatusCodes.Status400BadRequest, string.Empty);
            }
            throw;
        }
        return (StatusCodes.Status200OK, string.Empty);
    }
}