using System;
using Indice.Hosting.Tasks.Data;
using Indice.Hosting.Tasks.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Indice.Api.Data
{
    public class ExtendedTaskDbContext : TaskDbContext
    {
        public ExtendedTaskDbContext(DbContextOptions<ExtendedTaskDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);
            // https://www.npgsql.org/efcore/modeling/concurrency.html
            //builder.Entity<DbQMessage>().UseXminAsConcurrencyToken();
            var converter = new ValueConverter<byte[], uint>(x => BitConverter.ToUInt32(x, 0), x => BitConverter.GetBytes(x));
            builder.Entity<DbQMessage>().Property(x => x.RowVersion).HasColumnType("xid").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasConversion(converter);
        }
    }
}
