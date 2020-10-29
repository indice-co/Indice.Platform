using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Hosting.Tasks.Data
{
    class DbQTaskMap : IEntityTypeConfiguration<DbQTask>
    {
        public void Configure(EntityTypeBuilder<DbQTask> builder) {
            // Configure table name.
            builder.ToTable("QTask", "dbo");
            // Configure primary key.
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Type);
        }
    }
}
