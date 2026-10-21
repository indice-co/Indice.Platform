using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Ports;

/// <summary>
/// Turns a plain-text user reply into the typed response of a pending workflow port request, so a user who types the
/// answer (instead of using the port UI) still resumes the waiting workflow.
/// </summary>
public static class PortResponseCoercer
{
    /// <summary>Tries to build the <see cref="FunctionResultContent"/> answering <paramref name="pendingRequest"/> from <paramref name="text"/>.</summary>
    /// <param name="pendingRequest">The pending port request, as emitted by the workflow.</param>
    /// <param name="text">The user's plain-text reply.</param>
    /// <param name="result">When this method returns, contains the function result if the port is known; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the reply was coerced; otherwise, <see langword="false"/>.</returns>
    public static bool TryCoerce(FunctionCallContent pendingRequest, string? text, out FunctionResultContent? result) {
        result = null;
        text = text?.Trim() ?? string.Empty;
        object? response = pendingRequest.Name switch {
            nameof(OtpRequestPort.OtpRequest) => new OtpRequestPort.OtpResponse(GetRequestProperty(pendingRequest, nameof(OtpRequestPort.OtpRequest.ChallengeCode)) ?? string.Empty, text),
            nameof(ChallengeRequestPort) => new ChallengeRequestPort.ChallengeResponse(text),
            nameof(OperationRequestPort.OperationRequest) => new OperationRequestPort.OperationResponse(text),
            nameof(PaymentRequestPort.PaymentRequest) => new PaymentRequestPort.PaymentResponse(text),
            nameof(UserInputRequestPort.UserInputRequest) => new UserInputRequestPort.UserResponse(text),
            _ => null
        };
        if (response is null) {
            return false;
        }
        result = new FunctionResultContent(pendingRequest.CallId, JsonSerializer.SerializeToElement(response, response.GetType(), JsonSerializerOptions.Web));
        return true;
    }

    private static string? GetRequestProperty(FunctionCallContent pendingRequest, string name) {
        if (pendingRequest.Arguments?.TryGetValue("data", out var data) is not true || data is null) {
            return null;
        }
        var element = data as JsonElement? ?? JsonSerializer.SerializeToElement(data, data.GetType(), JsonSerializerOptions.Web);
        return FindString(element, name);
    }

    // The request data may arrive wrapped (e.g. a portable value envelope), so nested objects are searched too.
    private static string? FindString(JsonElement element, string name) {
        if (element.ValueKind != JsonValueKind.Object) {
            return null;
        }
        foreach (var property in element.EnumerateObject()) {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String) {
                return property.Value.GetString();
            }
        }
        foreach (var property in element.EnumerateObject()) {
            if (FindString(property.Value, name) is { } value) {
                return value;
            }
        }
        return null;
    }
}
