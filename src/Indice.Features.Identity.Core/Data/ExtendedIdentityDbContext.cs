using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Indice.Features.Identity.Core.Data;

/// <summary>An extended <see cref="DbContext"/> for the Identity framework.</summary>
public class ExtendedIdentityDbContext<TUser, TRole> : IdentityDbContext<TUser, TRole>
    where TUser : User, new()
    where TRole : Role, new()
{
    /// <summary>Creates a new instance of <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.</summary>
    /// <param name="dbContextOptions">The options to be used by a <see cref="DbContext"/>.</param>
    public ExtendedIdentityDbContext(DbContextOptions<ExtendedIdentityDbContext<TUser, TRole>> dbContextOptions) : base(dbContextOptions) {
#if DEBUG
        this.SeedAdminUser();
#endif

    }

    /// <summary>Creates a new instance of <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.</summary>
    /// <param name="dbContextOptions">The options to be used by a <see cref="DbContext"/>.</param>
    /// <param name="webHostEnvironment">Provides information about the web hosting environment an application is running in.</param>
    public ExtendedIdentityDbContext(DbContextOptions<ExtendedIdentityDbContext<TUser, TRole>> dbContextOptions, IWebHostEnvironment webHostEnvironment) : base(dbContextOptions) {
        if (webHostEnvironment.IsDevelopment() && Database.EnsureCreated()) {
            this.SeedAdminUser();
        }
    }

    /// <summary>Register extended configuration methods when the database is being created.</summary>
    /// <param name="modelBuilder">Provides a simple API surface for configuring a <see cref="DbContext"/>.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
    }
}
