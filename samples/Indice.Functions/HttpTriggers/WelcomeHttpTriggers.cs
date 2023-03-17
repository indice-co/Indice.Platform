using System.Net;
using Azure.Core.Serialization;
using Indice.Functions.Models;
using Indice.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Indice.Functions.Http;

public class WelcomeHttpTriggers
{
    [Function(FunctionNames.WelcomeHttpFunction)]
    public async static Task<HttpResponseData> Welcome(
        [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "welcome")] HttpRequestData request,
        FunctionContext executionContext
    ) {
        var logger = executionContext.GetLogger(FunctionNames.WelcomeHttpFunction);
        logger.LogInformation("Function '{FunctionName}' was triggered.", FunctionNames.WelcomeHttpFunction);
        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add(HeaderNames.Date, DateTime.Now.ToUniversalTime().ToString("R"));
        await response.WriteAsJsonAsync(new WelcomeMessage {
            Id = Guid.NewGuid(),
            Text = "Welcome to Azure functions using .NET 6"
        }, new JsonObjectSerializer(JsonSerializerOptionDefaults.GetDefaultSettings()));
        return response;
    }
}
