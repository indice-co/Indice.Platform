using Indice.Configuration;
using Indice.Features.Identity.SignInLogs.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Identity.SignInLogs.EntityFrameworkCore;

internal class DbSignInLogEntryMap : IEntityTypeConfiguration<DbSignInLogEntry>
{
    public DbSignInLogEntryMap(string schema) {
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    public string Schema { get; }

    public void Configure(EntityTypeBuilder<DbSignInLogEntry> builder) {
        // Configure table name and schema.
        builder.ToTable("SignInLog", Schema);
        // Configure primary key.
        builder.HasKey(x => x.Id);
        // Configure indexes.
        builder.HasIndex(x => x.ApplicationId);
        builder.HasIndex(x => x.ActionName);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.SessionId);
        builder.HasIndex(x => x.SubjectId);
        builder.HasIndex(x => x.SubjectName);
        // Configure properties.
        builder.Property(x => x.ActionName).HasMaxLength(TextSizePresets.M256);
        builder.Property(x => x.ApplicationId).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.ApplicationName).HasMaxLength(TextSizePresets.M512);
        builder.Property(x => x.CountryIsoCode).HasMaxLength(TextSizePresets.S08);
        builder.Property(x => x.Description).HasMaxLength(TextSizePresets.L2048);
        builder.Property(x => x.DeviceId).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.ExtraData).HasJsonConversion();
        builder.Property(x => x.GrantType).HasMaxLength(TextSizePresets.S32);
        builder.Property(x => x.IpAddress).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.Location).HasMaxLength(TextSizePresets.M512);
        builder.Property(x => x.RequestId).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.ResourceId).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.ResourceType).HasMaxLength(TextSizePresets.S64);
        builder.Property(x => x.SessionId).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.SubjectId).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.SubjectName).HasMaxLength(TextSizePresets.M512);
        builder.Property(x => x.Succeeded).IsRequired();
    }
}
