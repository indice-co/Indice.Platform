using Indice.AspNetCore.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.AspNetCore.Identity.Data.Mappings
{
    /// <summary>
    /// <see cref="IEntityTypeConfiguration{User}"/> for the <seealso cref="User"/> type.
    /// </summary>
    public class UserMap<TUser> : IEntityTypeConfiguration<TUser> where TUser: User
    {
        /// <summary>
        /// Configures the enity builder.
        /// </summary>
        /// <param name="entityBuilder"></param>
        public void Configure(EntityTypeBuilder<TUser> entityBuilder) {
            entityBuilder.ToTable(nameof(User), "auth");
            entityBuilder.HasMany(x => x.Claims).WithOne().HasForeignKey(x => x.UserId);
            entityBuilder.HasMany(x => x.Logins).WithOne().HasForeignKey(x => x.UserId);
            entityBuilder.HasMany(x => x.Roles).WithOne().HasForeignKey(x => x.UserId);
        }
    }
}
