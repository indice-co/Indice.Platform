using System;
using Indice.Hosting.Tasks.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Indice.Identity.Hosting
{
    public class ExtendedTaskDbContext : TaskDbContext
    {
        public ExtendedTaskDbContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);
            var converter = new ValueConverter<byte[], long>(x => BitConverter.ToInt64(x, 0), x => BitConverter.GetBytes(x));
            builder.Entity<DbQMessage>().Property(x => x.RowVersion).HasColumnName("xmin").HasColumnType("xid").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasConversion(converter);
        }
    }
}
