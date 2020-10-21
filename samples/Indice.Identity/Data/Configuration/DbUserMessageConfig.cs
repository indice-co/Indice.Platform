using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Identity.Data
{
    public class DbUserMessageConfig : IEntityTypeConfiguration<DbUserMessage>
    {
        public void Configure(EntityTypeBuilder<DbUserMessage> builder) {
            // Configure table name.
            builder.ToTable("UserMessage", "dbo");
            // Configure primary key.
            builder.HasKey(x => x.Id);
            // Configure properties.
            builder.Property(x => x.Message).HasMaxLength(512);
        }
    }
}
