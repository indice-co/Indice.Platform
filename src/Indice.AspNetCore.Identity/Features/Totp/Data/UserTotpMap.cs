using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Entity Framework mapping for type <see cref="UserTotp"/>.
    /// </summary>
    internal class UserTotpMap : IEntityTypeConfiguration<UserTotp>
    {
        /// <summary>
        /// Configure Entity Framework mapping for type <see cref="UserTotp"/>.
        /// </summary>
        /// <param name="builder">Provides a simple API for configuring an <see cref="EntityType"/>.</param>
        public void Configure(EntityTypeBuilder<UserTotp> builder) {
            // Configure table name and schema.
            builder.ToTable(nameof(UserTotp), "auth");
            // Configure primary key.
            builder.HasKey(x => new { x.UserId, x.Code });
        }
    }
}
