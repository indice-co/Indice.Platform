using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Extensions.Configuration.Database
{
    /// <summary>
    /// Entity Framework mapping for type <see cref="AppSetting"/>.
    /// </summary>
    public class AppSettingMap : IEntityTypeConfiguration<AppSetting>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<AppSetting> builder) {
            // Configure table name and schema.
            builder.ToTable(nameof(AppSetting), AppSetting.TableSchema);
            // Configure primary key.
            builder.HasKey(x => x.Key);
            // Configure fields.
            builder.Property(x => x.Key).HasMaxLength(512);
            builder.Property(x => x.Value).HasMaxLength(2048).IsRequired();
        }
    }
}
