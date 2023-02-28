using System.Collections.Generic;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Identity.Api.Configuration;

/// <summary>Options for configuring the IdentityServer API feature.</summary>
public class IdentityServerApiEndpointsOptions
{
    internal IServiceCollection Services;
    /// <summary>Options for the SMS sent when a user updates his phone number.</summary>
    public PhoneNumberOptions PhoneNumber { get; set; } = new PhoneNumberOptions();
    /// <summary>Options for the email sent when a user updates his email address.</summary>
    public EmailOptions Email { get; set; } = new EmailOptions();
    /// <summary>If true, it seeds the database with some initial data for users. Works only when environment is 'Development'. Default is false.</summary>
    public bool SeedDummyUsers { get; set; } = false;
    /// <summary>If true, various events (user or client created etc.) are raised from the API. Default is false.</summary>
    public bool CanRaiseEvents { get; set; } = false;
    /// <summary>A list of initial users to be inserted in the database on startup. Works only when environment is 'Development'.</summary>
    public IEnumerable<DbUser> InitialUsers { get; set; } = new List<DbUser>();
    /// <summary>A list of custom claim types to be inserted in the database on startup. Works only when environment is 'Development'.</summary>
    public IEnumerable<DbClaimType> CustomClaims { get; set; } = new List<DbClaimType>();
    /// <summary>Disables the cache for all the endpoints in the IdentityServer API. Defaults to false.</summary>
    public bool DisableCache { get; set; } = false;
}