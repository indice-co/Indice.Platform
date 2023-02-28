using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Identity.Core.Data.Mappings;

/// <summary>Entity Framework mapping for type <see cref="DbUserPassword"/>.</summary>
/// <typeparam name="TUser">The type of user.</typeparam>
internal class DbUserPasswordMap<TUser> : IEntityTypeConfiguration<DbUserPassword> where TUser : DbUser
{
    /// <summary>Configure Entity Framework mapping for type <see cref="DbUserPassword"/>.</summary>
    /// <param name="builder"></param>
    public void Configure(EntityTypeBuilder<DbUserPassword> builder) {
        // Configure table name and schema.
        builder.ToTable(nameof(DbUserPassword), "auth");
        // Configure primary key.
        builder.HasKey(x => x.Id);
        // Configure relationships.
        builder.HasOne<TUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
