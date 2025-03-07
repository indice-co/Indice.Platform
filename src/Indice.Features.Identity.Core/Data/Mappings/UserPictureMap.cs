using Indice.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Identity.Core.Data.Mappings;

/// <summary>Entity Framework mapping for type <see cref="UserPicture"/>.</summary>
/// <typeparam name="TUser">The type of user.</typeparam>
internal class UserPictureMap<TUser> : IEntityTypeConfiguration<UserPicture> where TUser : User
{
    /// <summary>Configure Entity Framework mapping for type <see cref="UserPassword"/>.</summary>
    /// <param name="builder"></param>
    public void Configure(EntityTypeBuilder<UserPicture> builder) {
        // Configure table name and schema.
        builder.ToTable(nameof(UserPicture), "auth");
        // Configure primary key.
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ContentType).HasMaxLength(TextSizePresets.M256).IsRequired();
        builder.Property(x => x.PictureKey).HasMaxLength(TextSizePresets.S64).IsRequired();
        builder.HasIndex(x => x.PictureKey);
        // Configure relationships.
        builder.HasOne<TUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
