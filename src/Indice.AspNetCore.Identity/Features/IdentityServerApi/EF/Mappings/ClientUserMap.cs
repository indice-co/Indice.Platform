using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Entity Framework mapping for type <see cref="ClientUser"/>.
    /// </summary>
    internal class ClientUserMap : IEntityTypeConfiguration<ClientUser>
    {
        /// <summary>
        /// Configure Entity Framework mapping for type <see cref="ClientUser"/>.
        /// </summary>
        /// <param name="builder"></param>
        public void Configure(EntityTypeBuilder<ClientUser> builder) {
            // Configure table name and schema.
            builder.ToTable(nameof(ClientUser), "config");
            // Configure primary key.
            builder.HasKey(x => new { x.ClientId, x.UserId });
            // Configure relationships.
            builder.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId);
        }
    }
}
