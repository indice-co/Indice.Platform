using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Hosting.EntityFrameworkCore
{
    /// <summary>
    /// EF Core confirugation for <see cref="DbQMessage"/> entity.
    /// </summary>
    public sealed class DbQMessageMap : IEntityTypeConfiguration<DbQMessage>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<DbQMessage> builder) {
            // Configure table name.
            builder.ToTable("QMessage", "work");
            // Configure primary key.
            builder.HasKey(x => x.Id);
            // Configure indexes.
            builder.HasIndex(x => x.QueueName);
            // Configure properties.
            builder.Property(x => x.RowVersion).IsRowVersion();
        }
    }
}
