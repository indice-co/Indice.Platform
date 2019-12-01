using Indice.AspNetCore.Identity.Models;
using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.AspNetCore.Identity.Data.Mappings
{
    /// <summary>
    /// Entity Framework mapping for type <see cref="SystemSettings"/>.
    /// </summary>
    internal class SystemSettingsMap : IEntityTypeConfiguration<SystemSettings>
    {
        public void Configure(EntityTypeBuilder<SystemSettings> builder) {
            // Configure table name and schema.
            builder.ToTable(nameof(SystemSettings), "auth");
            // Configure primary key.
            builder.HasKey(x => x.Id);
            // Configure fields.
            builder.Property(x => x.Key).HasMaxLength(TextSizePresets.M128).IsRequired();
            builder.Property(x => x.Value).HasMaxLength(TextSizePresets.L2048).IsRequired();
        }
    }
}
