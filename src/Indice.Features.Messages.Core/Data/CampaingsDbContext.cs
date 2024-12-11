using Indice.Features.Messages.Core.Data.Mappings;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.Core.Data;

/// <summary>The <see cref="DbContext"/> for Campaigns API feature.</summary>
public class CampaignsDbContext : DbContext
{
    /// <summary>Creates a new instance of <see cref="CampaignsDbContext"/>.</summary>
    /// <param name="options">The options to be used by <see cref="CampaignsDbContext"/>.</param>
    public CampaignsDbContext(DbContextOptions<CampaignsDbContext> options) : base(options) {

    }

    /// <summary>Campaign attachments table.</summary>
    public DbSet<DbAttachment> Attachments { get; set; }
    /// <summary>Campaigns table.</summary>
    public DbSet<DbCampaign> Campaigns { get; set; }
    /// <summary>Message types table.</summary>
    public DbSet<DbMessageType> MessageTypes { get; set; }
    /// <summary>Message senders table.</summary>
    public DbSet<DbMessageSender> MessageSenders { get; set; }
    /// <summary>Inbox messages table.</summary>
    public DbSet<DbMessage> Messages { get; set; }
    /// <summary>Campaign hits table.</summary>
    public DbSet<DbHit> Hits { get; set; }
    /// <summary>Templates table.</summary>
    public DbSet<DbTemplate> Templates { get; set; }
    /// <summary>Distribution lists table.</summary>
    public DbSet<DbDistributionList> DistributionLists { get; set; }
    /// <summary>Contacts table.</summary>
    public DbSet<DbContact> Contacts { get; set; }
    /// <summary>A table that associates contacts and distribution lists.</summary>
    public DbSet<DbDistributionListContact> ContactDistributionLists { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        var schemaName = Database.GetService<DatabaseSchemaNameResolver>().GetSchemaName();
        modelBuilder.ApplyConfiguration(new DbAttachmentMap(schemaName));
        modelBuilder.ApplyConfiguration(new DbCampaignMap(schemaName));
        modelBuilder.ApplyConfiguration(new DbDistributionListContactMap(schemaName));
        modelBuilder.ApplyConfiguration(new DbContactMap(schemaName));
        modelBuilder.ApplyConfiguration(new DbDistributionListMap(schemaName));
        modelBuilder.ApplyConfiguration(new DbHitMap(schemaName));
        modelBuilder.ApplyConfiguration(new DbMessageMap(schemaName));
        modelBuilder.ApplyConfiguration(new DbMessageTypeMap(schemaName));
        modelBuilder.ApplyConfiguration(new DbTemplateMap(schemaName));
        modelBuilder.ApplyConfiguration(new DbMessageSenderMap(schemaName));
        if (Database.IsSqlServer()) {
            modelBuilder.Entity<DbAttachment>().Property(x => x.Data).HasColumnType("image");
        } else if (Database.IsNpgsql()) {
            modelBuilder.Entity<DbAttachment>().Property(x => x.Data).HasColumnType("bytea");
        }
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
        OnBeforeSaving();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Runs code before persisting entities to the database.</summary>
    protected void OnBeforeSaving() {
        var auditableEntries = ChangeTracker.Entries().Where(entry => typeof(DbAuditableEntity).IsAssignableFrom(entry.Metadata.ClrType));
        var userName = Database.GetService<UserNameAccessorAggregate>().Resolve();
        if (string.IsNullOrWhiteSpace(userName)) {
            return;
        }
        foreach (var entry in auditableEntries) {
            var auditableEntry = (DbAuditableEntity)entry.Entity;
            if (entry.State == EntityState.Added) {
                auditableEntry.CreatedAt = DateTimeOffset.UtcNow;
                auditableEntry.CreatedBy ??= userName;
            }
            if (entry.State == EntityState.Modified) {
                auditableEntry.UpdatedAt = DateTimeOffset.UtcNow;
                auditableEntry.UpdatedBy ??= userName;
            }
        }
    }
}
