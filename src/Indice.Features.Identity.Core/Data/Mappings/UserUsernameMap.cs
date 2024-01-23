using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Identity.Core.Data.Mappings;

internal class UserUsernameMap<TUser> : IEntityTypeConfiguration<UserUsername> where TUser : User
{
    public void Configure(EntityTypeBuilder<UserUsername> builder) {
        builder.ToTable(nameof(UserUsername), "auth");
        builder.HasKey(x => x.Id);
        builder
            .HasOne<TUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}