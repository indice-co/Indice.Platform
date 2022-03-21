using System;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.AspNetCore.Features.Campaigns.Data
{

    internal class DbContactMap : IEntityTypeConfiguration<DbContact>
    {
        public DbContactMap(string schemaName) {
            SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        }

        public string SchemaName { get; }

        public void Configure(EntityTypeBuilder<DbContact> builder) {
            builder.ToTable("Contact", SchemaName);
            // Configure primary key.
            builder.HasKey(x => x.Id);
            // Configure properties.
            builder.Property(x => x.Salutation).HasMaxLength(TextSizePresets.S32);
            builder.Property(x => x.FirstName).HasMaxLength(TextSizePresets.M128);
            builder.Property(x => x.LastName).HasMaxLength(TextSizePresets.M128);
            builder.Property(x => x.FullName).HasMaxLength(TextSizePresets.M256);
            builder.Property(x => x.PhoneNumber).HasMaxLength(TextSizePresets.S64);
            builder.Property(x => x.Email).HasMaxLength(TextSizePresets.S64);
            builder.Property(x => x.DeviceId).HasMaxLength(TextSizePresets.M128);
            builder.Property(x => x.RecipientId).HasMaxLength(TextSizePresets.S64);
            // Configure indexes.
            builder.HasIndex(x => x.RecipientId);
            // Configure relationships.
            builder.HasOne<DbDistributionList>().WithMany(x => x.Contacts).HasForeignKey(x => x.DistributionListId);
        }
    }
}
