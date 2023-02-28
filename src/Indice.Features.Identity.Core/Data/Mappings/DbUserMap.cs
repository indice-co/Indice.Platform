using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Identity.Core.Data.Mappings;

/// <summary>Entity Framework mapping for type <see cref="DbUser"/>.</summary>
/// <typeparam name="TUser">The type of user.</typeparam>
internal class DbUserMap<TUser> : IEntityTypeConfiguration<TUser> where TUser : DbUser
{
    /// <summary>Configure Entity Framework mapping for type <see cref="DbUser"/>.</summary>
    /// <param name="entityBuilder"></param>
    public void Configure(EntityTypeBuilder<TUser> entityBuilder) {
        // Configure table name and schema.
        entityBuilder.ToTable(nameof(DbUser), "auth");
        // Configure indexes.
        entityBuilder.HasIndex(x => x.UserName);
        entityBuilder.HasIndex(x => x.CreateDate);
        entityBuilder.HasIndex(x => x.LastSignInDate);
        // Configure relationships.
        entityBuilder.HasMany(x => x.Claims).WithOne().HasForeignKey(x => x.UserId);
        entityBuilder.HasMany(x => x.Logins).WithOne().HasForeignKey(x => x.UserId);
        entityBuilder.HasMany(x => x.Roles).WithOne().HasForeignKey(x => x.UserId);
    }
}
