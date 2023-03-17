using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Identity.SignInLogs.EntityFrameworkCore;

/// <summary>Entity Framework Core mapping for type <see cref="DbSignInLogEntry"/>.</summary>
public class DbSignInLogEntryMap : IEntityTypeConfiguration<DbSignInLogEntry>
{
    /// <summary>Configure Entity Framework mapping for type <see cref="DbSignInLogEntry"/>.</summary>
    /// <param name="builder">Provides a simple API for configuring an <see cref="IMutableEntityType" />.</param>
    public void Configure(EntityTypeBuilder<DbSignInLogEntry> builder) {
        // Configure table name and schema.
        builder.ToTable("SignInLog", "dbo");
        // Configure primary key.
        builder.HasKey(x => x.Id);
        // Configure indexes.
        builder.HasIndex(x => x.ApplicationId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.SessionId);
        builder.HasIndex(x => x.SubjectId);
        // Configure properties.
        builder.Property(x => x.ActionName).HasMaxLength(TextSizePresets.M256);
        builder.Property(x => x.ApplicationId).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.ApplicationName).HasMaxLength(TextSizePresets.M512);
        builder.Property(x => x.Description).HasMaxLength(TextSizePresets.L2048);
        builder.Property(x => x.ExtraData).HasJsonConversion();
        builder.Property(x => x.IpAddress).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.Location).HasMaxLength(TextSizePresets.M512);
        builder.Property(x => x.RequestId).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.ResourceId).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.ResourceType).HasMaxLength(TextSizePresets.S64);
        builder.Property(x => x.SessionId).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.SubjectId).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.SubjectName).HasMaxLength(TextSizePresets.M512);
        builder.Property(x => x.Succedded).IsRequired();
    }
}
