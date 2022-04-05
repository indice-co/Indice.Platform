using Indice.Configuration;
using Indice.Features.Messages.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings
{
    /// <summary>
    /// Configuration for <see cref="DbAttachment"/> entity.
    /// </summary>
    public class DbAttachmentMap : IEntityTypeConfiguration<DbAttachment>
    {
        /// <summary>
        /// Creates a new instance of <see cref="DbAttachmentMap"/>.
        /// </summary>
        /// <param name="schemaName">The schema name.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public DbAttachmentMap(string schemaName) {
            SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        }

        private string SchemaName { get; }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<DbAttachment> builder) {
            // Configure table name.
            builder.ToTable("Attachment", SchemaName);
            // Configure primary key.
            builder.HasKey(x => x.Id);
            // Configure properties.
            builder.Property(x => x.Name).HasMaxLength(TextSizePresets.M256).IsRequired();
            builder.Property(x => x.ContentType).HasMaxLength(TextSizePresets.M256).IsRequired();
            builder.Property(x => x.FileExtension).HasMaxLength(TextSizePresets.S08).IsRequired();
            builder.Property(x => x.Data).HasColumnType("image");
            builder.Ignore(x => x.Uri);
        }
    }
}
