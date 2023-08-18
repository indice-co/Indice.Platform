using System.Diagnostics;
using Indice.Features.Media.AspNetCore.Data.Mappings;
using Indice.Features.Media.AspNetCore.Data.Models;
using Indice.Features.Media.AspNetCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Media.Data;

/// <summary>The <see cref="DbContext"/> for Media Management API feature.</summary>
public class MediaDbContext : DbContext
{
    /// <summary>Creates a new instance of <see cref="MediaDbContext"/>.</summary>
    /// <param name="options">The options to be used by <see cref="MediaDbContext"/>.</param>
    public MediaDbContext(DbContextOptions<MediaDbContext> options) : base(options) {
        if (Debugger.IsAttached) {
            Database.EnsureCreated();
        }
    }

    /// <summary>Media folders table.</summary>
    public DbSet<DbFolder> Folders { get; set; }
    /// <summary>Media files table.</summary>
    public DbSet<DbFile> Files { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder) {
        builder.ApplyConfiguration(new DbFolderMap());
        builder.ApplyConfiguration(new DbFileMap());
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
        OnBeforeSaving();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Runs code before persisting entities to the database.</summary>
    protected void OnBeforeSaving() {
        var userNameAccessor = Database.GetService<IUserNameAccessor>();
        if (userNameAccessor is null) {
            return;
        }
        var auditableEntries = ChangeTracker.Entries().Where(entry => typeof(DbAuditableEntity).IsAssignableFrom(entry.Metadata.ClrType));
        foreach (var entry in auditableEntries) {
            var auditableEntry = (DbAuditableEntity)entry.Entity;
            if (entry.State == EntityState.Added) {
                auditableEntry.CreatedAt = DateTimeOffset.UtcNow;
                auditableEntry.CreatedBy ??= userNameAccessor.Resolve();
            }
            if (entry.State == EntityState.Modified) {
                auditableEntry.UpdatedAt = DateTimeOffset.UtcNow;
                auditableEntry.UpdatedBy ??= userNameAccessor.Resolve();
            }
        }
    }
}
