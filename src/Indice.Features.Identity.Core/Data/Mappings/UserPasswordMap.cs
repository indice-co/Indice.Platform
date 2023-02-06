using Indice.Features.Identity.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Identity.Core.Data.Mappings
{
    /// <summary>Entity Framework mapping for type <see cref="UserPassword"/>.</summary>
    /// <typeparam name="TUser">The type of user.</typeparam>
    internal class UserPasswordMap<TUser> : IEntityTypeConfiguration<UserPassword> where TUser : User
    {
        /// <summary>Configure Entity Framework mapping for type <see cref="UserPassword"/>.</summary>
        /// <param name="builder"></param>
        public void Configure(EntityTypeBuilder<UserPassword> builder) {
            // Configure table name and schema.
            builder.ToTable(nameof(UserPassword), "auth");
            // Configure primary key.
            builder.HasKey(x => x.Id);
            // Configure relationships.
            builder.HasOne<TUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
