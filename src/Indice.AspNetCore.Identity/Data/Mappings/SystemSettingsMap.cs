using Indice.AspNetCore.Identity.Models;
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
            // Configure fields.
            builder.Property(x => x.OptionsJson).HasColumnName(nameof(SystemSettings.Options)).IsRequired();
            // Ignored properties.
            builder.Ignore(x => x.Options);
        }
    }
}
