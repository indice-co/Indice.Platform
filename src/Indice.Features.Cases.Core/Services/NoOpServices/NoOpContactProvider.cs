using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Security;
using Indice.Serialization;
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

    public Task<ResultSet<Contact>> GetListAsync(ClaimsPrincipal user, ListOptions<ContactFilter> listOptions) =>
        Environment.IsDevelopment() ?
            Task.FromResult(new ResultSet<Contact>([ToContact(user), JohnDoe(Options)], 2)) :
            Task.FromResult(new ResultSet<Contact>([ToContact(user)], 1));


    public Task<Contact?> GetByReferenceAsync(ClaimsPrincipal user, string reference, string caseTypeCode) => 
        Task.FromResult<Contact?>(ToContact(user));

    private Contact ToContact(ClaimsPrincipal claimsPrincipal) => new () {
        UserId = claimsPrincipal.FindFirstValue(Options.UserClaimType),
        Email = claimsPrincipal.FindFirstValue(BasicClaimTypes.Email),
        Reference = claimsPrincipal.FindFirstValue(Options.ReferenceIdClaimType) ?? claimsPrincipal.FindFirstValue(Options.UserClaimType),
        FirstName = claimsPrincipal.FindFirstValue(BasicClaimTypes.GivenName),
        LastName = claimsPrincipal.FindFirstValue(BasicClaimTypes.FamilyName),
        GroupId = claimsPrincipal.FindFirstValue(Options.GroupIdClaimType),
        Tin = claimsPrincipal.FindFirstValue(Options.TinClaimType),
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
        },
        // used to initialize an new case instance the SampleAddress caseType from the selected contact as Owner.
        FormData = new {
            postOfficeBox = "123",
            streetAddress = "456 Main St",
            locality = "Cityville",
            region = "State",
            postalCode = "12345",
            countryName = "Country"
        }    
    };
}
