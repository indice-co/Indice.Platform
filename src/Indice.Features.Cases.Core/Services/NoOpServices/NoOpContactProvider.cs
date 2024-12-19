using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Security;
using Indice.Serialization;
using Indice.Types;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Core.Services.NoOpServices;

internal class NoOpContactProvider : IContactProvider
{
    public CasesOptions Options { get; }

    public NoOpContactProvider(IOptions<CasesOptions> options) {
        Options = options.Value;
    }

    public Task<ResultSet<Contact>> GetListAsync(ClaimsPrincipal user, ListOptions<ContactFilter> listOptions) => 
        Task.FromResult(new ResultSet<Contact>([ToContact(user)], 1));


    public Task<JsonNode?> GetByReferenceAsync(ClaimsPrincipal user, string reference, string caseTypeCode) => 
        Task.FromResult(JsonSerializer.SerializeToNode(ToContact(user), JsonSerializerOptionDefaults.GetDefaultSettings()));

    private Contact ToContact(ClaimsPrincipal claimsPrincipal) => new () {
        UserId = claimsPrincipal.FindSubjectId(),
        Email = claimsPrincipal.FindFirstValue(BasicClaimTypes.Email),
        Reference = claimsPrincipal.FindFirstValue(Options.UserClaimType),
        FirstName = claimsPrincipal.FindFirstValue(BasicClaimTypes.GivenName),
        LastName = claimsPrincipal.FindFirstValue(BasicClaimTypes.FamilyName),
        GroupId = claimsPrincipal.FindFirstValue(Options.GroupIdClaimType),
        Tin = claimsPrincipal.FindFirstValue(BasicClaimTypes.Tin),
    };
}
