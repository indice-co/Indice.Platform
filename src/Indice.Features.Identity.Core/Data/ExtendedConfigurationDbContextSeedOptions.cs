using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Options;
using Indice.Features.Identity.Core.Data.Mappings;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Identity.Core.Data;

/// <summary>Extended DbContext for the IdentityServer configuration data.</summary>
public class ExtendedConfigurationDbContext : ConfigurationDbContext<ExtendedConfigurationDbContext>
{
    /// <summary>Creates a new instance of <see cref="ExtendedConfigurationDbContext"/>.</summary>
    /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
    /// <param name="storeOptions">Options for configuring the <see cref="ExtendedConfigurationDbContext"/>.</param>
    /// <param name="webHostEnvironment">Provides information about the web hosting environment an application is running in.</param>
    public ExtendedConfigurationDbContext(
        DbContextOptions<ExtendedConfigurationDbContext> options,
        ConfigurationStoreOptions storeOptions,
        IWebHostEnvironment webHostEnvironment
    ) : base(options, storeOptions) {
        
    }

    /// <summary>A table that contains the association between a client and a user.</summary>
    public DbSet<ClientUser> ClientUsers { get; set; }
    /// <summary>A table that contains custom data for a client secret.</summary>
    public DbSet<ClientSecretExtended> ClientSecretExtras { get; set; }
    /// <summary>A table that contains all the available claim types of the application.</summary>
    public DbSet<ClaimType> ClaimTypes { get; set; }

    /// <summary>Register extended configuration methods when the database is being created.</summary>
    /// <param name="modelBuilder">Provides a simple API surface for configuring a <see cref="DbContext"/>.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ClientUserMap());
        modelBuilder.ApplyConfiguration(new ClientSecretExtendedMap());
        modelBuilder.ApplyConfiguration(new ClaimTypeMap());
    }
}
