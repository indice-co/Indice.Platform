using Indice.AspNetCore.Identity.Entities;
using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.AspNetCore.Identity.EntityFrameworkCore
{
    /// <summary>
    /// Entity Framework mapping for type <see cref="AppSetting"/>.
    /// </summary>
    internal class AppSettingMap : IEntityTypeConfiguration<AppSetting>
    {
        public void Configure(EntityTypeBuilder<AppSetting> builder) {
            // Configure table name and schema.
            builder.ToTable(nameof(AppSetting), AppSetting.TableSchema);
            // Configure primary key.
            builder.HasKey(x => x.Key);
            // Configure fields.
            builder.Property(x => x.Key).HasMaxLength(TextSizePresets.M512);
            builder.Property(x => x.Value).HasMaxLength(TextSizePresets.L2048).IsRequired();
        }
    }
}
