using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;

namespace Indice.Features.Cases.Core.Services.NoOpServices;

internal class NoOpCaseDataInitializer : ICaseDataInitializer
{
    public Task<JsonNode?> InitializeAsync(UserActor user, string caseTypeCode, Contact? contact = null) =>
        Task.FromResult(JsonNode.Parse("{}"));
}