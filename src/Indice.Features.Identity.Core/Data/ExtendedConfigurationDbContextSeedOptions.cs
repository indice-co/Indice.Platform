using Duende.IdentityServer.EntityFramework.DbContexts;
using Indice.Features.Identity.Core.Data.Mappings;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Identity.Core.Data;

/// <summary>Extended DbContext for the IdentityServer configuration data.</summary>
public class ExtendedConfigurationDbContext : ConfigurationDbContext<ExtendedConfigurationDbContext>
{
    /// <summary>Creates a new instance of <see cref="ExtendedConfigurationDbContext"/>.</summary>
    /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
    public ExtendedConfigurationDbContext(DbContextOptions<ExtendedConfigurationDbContext> options) 
        : base(options) {    
    }
    /// <summary>A table that contains the association between a client and a user.</summary>
    public DbSet<ClientUser> ClientUsers { get; set; } = null!;
    /// <summary>A table that contains custom data for a client secret.</summary>
    public DbSet<ClientSecretExtended> ClientSecretExtras { get; set; } = null!;
    /// <summary>A table that contains all the available claim types of the application.</summary>
    public DbSet<ClaimType> ClaimTypes { get; set; } = null!;

    /// <summary>Register extended configuration methods when the database is being created.</summary>
    /// <param name="modelBuilder">Provides a simple API surface for configuring a <see cref="DbContext"/>.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ClientUserMap());
        modelBuilder.ApplyConfiguration(new ClientSecretExtendedMap());
        modelBuilder.ApplyConfiguration(new ClaimTypeMap());
    }
}
