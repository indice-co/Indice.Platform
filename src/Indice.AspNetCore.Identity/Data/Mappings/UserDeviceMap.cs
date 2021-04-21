using Indice.AspNetCore.Identity.Data.Models;
using Microsoft.EntityFrameworkCore;
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
        /// <param name="builder"></param>
        public void Configure(EntityTypeBuilder<UserDevice> builder) {
            // Configure table name and schema.
            builder.ToTable(nameof(UserDevice), "auth");
            // Configure primary key.
            builder.HasKey(x => x.Id);
            //Device name length
            builder.Property(x => x.DeviceName).HasMaxLength(256);
            // Configure relationships.
            builder.HasOne<TUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
