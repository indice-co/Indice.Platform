﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Hosting.Tasks.Data
{
    /// <summary>
    /// EF Core confirugation for <see cref="DbLock"/> entity.
    /// </summary>
    public sealed class DbLockMap : IEntityTypeConfiguration<DbLock>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<DbLock> builder) {
            // Configure table name.
            builder.ToTable("Lock", "work");
            // Configure primary key.
            builder.HasKey(x => x.Name);
            // Configure properties.
            builder.Property(x => x.Name).HasMaxLength(256);
        }
    }
}
