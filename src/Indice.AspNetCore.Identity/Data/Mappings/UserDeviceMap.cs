using Indice.AspNetCore.Identity.Data.Models;
using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.AspNetCore.Identity.Data
{
    /// <summary>
    /// Entity Framework mapping for type <see cref="UserDevice"/>.
    /// </summary>
    /// <typeparam name="TUser">The type of user.</typeparam>
    internal class UserDeviceMap<TUser> : IEntityTypeConfiguration<UserDevice> where TUser : User
    {
        /// <summary>
        /// Configure Entity Framework mapping for type <see cref="UserDevice"/>.
        /// </summary>
        /// <param name="builder">Provides a simple API for configuring an <see cref="IMutableEntityType" />.</param>
        public void Configure(EntityTypeBuilder<UserDevice> builder) {
            // Configure table name and schema.
            builder.ToTable(nameof(UserDevice), "auth");
            // Configure primary key.
            builder.HasKey(x => x.Id);
            builder.HasAlternateKey(x => new { x.DeviceId, x.UserId });
            // Configure indexes.
            builder.HasIndex(x => x.DeviceId);
            // Configure properties.
            builder.Property(x => x.Name).HasMaxLength(TextSizePresets.M256);
            builder.Property(x => x.UserId).IsRequired();
            builder.Property(x => x.DeviceId).IsRequired();
            builder.Property(x => x.Data).HasJsonConversion();
            builder.Property(x => x.Tags).HasStringArrayConversion();
            builder.Property(x => x.PnsHandle).HasMaxLength(TextSizePresets.M256);
            // Configure relationships.
            builder.HasOne(x => x.User).WithMany(x => x.Devices).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
