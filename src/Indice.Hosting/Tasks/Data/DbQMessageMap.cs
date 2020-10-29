using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Hosting.Tasks.Data
{
    class DbQMessageMap : IEntityTypeConfiguration<DbQMessage>
    {
        public void Configure(EntityTypeBuilder<DbQMessage> builder) {
            // Configure table name.
            builder.ToTable("QMessage", "dbo");
            // Configure primary key.
            builder.HasKey(x => x.Id);
            builder.Property(x => x.RowVersion).IsRowVersion();
            builder.HasIndex(x => x.QueueName);
        }
    }
}
