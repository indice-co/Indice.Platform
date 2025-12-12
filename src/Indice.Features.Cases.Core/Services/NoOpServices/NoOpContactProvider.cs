using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Core.Services.NoOpServices;

internal class NoOpContactProvider : IContactProvider
{
    public CasesOptions Options { get; }
    public IHostEnvironment Environment { get; }

    public NoOpContactProvider(IOptions<CasesOptions> options, IHostEnvironment environment) {
        Options = options.Value;
        Environment = environment;
    }

    public Task<ResultSet<Contact>> GetListAsync(UserActor user, ListOptions<ContactFilter> listOptions) =>
        Environment.IsDevelopment() ?
            Task.FromResult(new ResultSet<Contact>([ToContact(user), JohnDoe(Options)], 2)) :
            Task.FromResult(new ResultSet<Contact>([ToContact(user)], 1));


    public Task<Contact?> GetByReferenceAsync(UserActor user, string reference) =>
        Task.FromResult<Contact?>(ToContact(user));

    private Contact ToContact(UserActor workflowActor) => new () {
        UserId = workflowActor.Id,
        Email = workflowActor.Email,
        Reference = workflowActor.Reference ?? workflowActor.Id,
        FirstName = workflowActor.Name,
        //todo check what to do with LastName
        //LastName = workflowActor.FindFirstValue(BasicClaimTypes.FamilyName),
        GroupId = workflowActor.GroupId,
        Tin = workflowActor.Tin,
    };

    public static Contact JohnDoe(CasesOptions options) => new() {
        FirstName = "John",
        LastName = "Doe",
        Email = "john.doe@indice.gr",
        PhoneNumber = "2101234567",
        UserId = "6a4bbee1-53c9-404c-b09f-db134688df6f",
        Reference = "0000000",
        Tin = "999999999",
        GroupId = "010",
        Metadata = new () {
            [options.TinClaimType] = "999999999",
            [options.GroupIdClaimType] = "010",
        }
    };
}
