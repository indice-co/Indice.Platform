﻿using Indice.Hosting.Data.Models;
using Indice.Hosting.Services;
using Microsoft.EntityFrameworkCore;

namespace Indice.Hosting.Data;

/// <summary>A <see cref="DbContext"/> for hosting multiple <see cref="IMessageQueue{T}"/>.</summary>
public class TaskDbContext : DbContext
{
    /// <summary>Creates a new instance of <see cref="TaskDbContext"/>.</summary>
    /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options) { }

    /// <summary>Creates a new instance of <see cref="TaskDbContext"/>.</summary>
    /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
    protected TaskDbContext(DbContextOptions options) : base(options) { }

    /// <summary>Queue messages.</summary>
    public DbSet<DbQMessage> Queue { get; set; }
    /// <summary>Tasks.</summary>
    public DbSet<DbScheduledTask> Tasks { get; set; }
    /// <summary>Locks.</summary>
    public DbSet<DbLock> Locks { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new DbQMessageMap());
        builder.ApplyConfiguration(new DbScheduledTaskMap());
        builder.ApplyConfiguration(new DbLockMap());
    }

}
