using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Hosting.Tasks.Data
{
    class DbLockMap : IEntityTypeConfiguration<DbLock>
    {
        public void Configure(EntityTypeBuilder<DbLock> builder) {
            // Configure table name.
            builder.ToTable("Lock", "work");
            // Configure primary key.
            builder.HasKey(x => new { x.Id, x.Name });
            builder.Property(x => x.Name).HasMaxLength(256);
        }
    }
}
