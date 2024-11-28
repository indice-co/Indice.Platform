﻿using Indice.Configuration;
using Indice.EntityFrameworkCore;
using Indice.Extensions.Configuration.Database;
using Indice.Extensions.Configuration.Database.Data;
using Indice.Extensions.Configuration.Database.Data.Models;
using Indice.Features.Risk.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Risk.Core.Data;

/// <summary>A <see cref="DbContext"/> for persisting events and their related data.</summary>
public class RiskDbContext : DbContext, IAppSettingsDbContext
{
    /// <summary>Creates a new instance of <see cref="RiskDbContext"/> class.</summary>
    /// <param name="dbContextOptions"></param>
    public RiskDbContext(DbContextOptions<RiskDbContext> dbContextOptions) : base(dbContextOptions) {
    }

    /// <summary>Risk events table.</summary>
    public DbSet<RiskEvent> RiskEvents => Set<RiskEvent>();

    /// <summary>Risk results table.</summary>
    public DbSet<DbAggregateRuleExecutionResult> RiskResults => Set<DbAggregateRuleExecutionResult>();

    /// <summary>
    /// Risk rules definitions table.
    /// </summary>
    public DbSet<DbAppSetting> AppSettings { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        // Risk event configuration.
        modelBuilder.Entity<RiskEvent>().ToTable(nameof(RiskEvent));
        modelBuilder.Entity<RiskEvent>().HasKey(x => x.Id);
        modelBuilder.Entity<RiskEvent>().HasIndex(x => x.SubjectId);
        modelBuilder.Entity<RiskEvent>().Property(x => x.Amount).HasColumnType("money");
        modelBuilder.Entity<RiskEvent>().Property(x => x.IpAddress).HasMaxLength(TextSizePresets.M128);
        modelBuilder.Entity<RiskEvent>().Property(x => x.SubjectId).HasMaxLength(TextSizePresets.M256).IsRequired();
        modelBuilder.Entity<RiskEvent>().Property(x => x.Name).HasMaxLength(TextSizePresets.M256);
        modelBuilder.Entity<RiskEvent>().Property(x => x.Type).HasMaxLength(TextSizePresets.M256).IsRequired();
        modelBuilder.Entity<RiskEvent>().Property(x => x.Data).HasJsonConversion();
        modelBuilder.Entity<RiskEvent>().Property(x => x.SourceId).HasMaxLength(TextSizePresets.M256);
        modelBuilder.Entity<RiskEvent>().Property(x => x.SourceTransId).HasMaxLength(TextSizePresets.M128);
        // Risk Result configuration.
        modelBuilder.Entity<DbAggregateRuleExecutionResult>().ToTable("RiskResult");
        modelBuilder.Entity<DbAggregateRuleExecutionResult>().HasKey(x => x.Id);
        modelBuilder.Entity<DbAggregateRuleExecutionResult>().HasIndex(x => x.SubjectId);
        modelBuilder.Entity<DbAggregateRuleExecutionResult>().Property(x => x.EventId).HasMaxLength(TextSizePresets.M128);
        modelBuilder.Entity<DbAggregateRuleExecutionResult>().Property(x => x.Amount).HasColumnType("money");
        modelBuilder.Entity<DbAggregateRuleExecutionResult>().Property(x => x.IpAddress).HasMaxLength(TextSizePresets.M128);
        modelBuilder.Entity<DbAggregateRuleExecutionResult>().Property(x => x.SubjectId).HasMaxLength(TextSizePresets.M256).IsRequired();
        modelBuilder.Entity<DbAggregateRuleExecutionResult>().Property(x => x.Name).HasMaxLength(TextSizePresets.M256);
        modelBuilder.Entity<DbAggregateRuleExecutionResult>().Property(x => x.Type).HasMaxLength(TextSizePresets.M256).IsRequired();
        modelBuilder.Entity<DbAggregateRuleExecutionResult>().Property(x => x.Data).HasJsonConversion();
        modelBuilder.Entity<DbAggregateRuleExecutionResult>().Property(x => x.NumberOfRulesExecuted);
        modelBuilder.Entity<DbAggregateRuleExecutionResult>().Property(x => x.Results).HasJsonConversion();
        modelBuilder.Entity<DbAggregateRuleExecutionResult>().Property(x => x.RiskScore).IsRequired();
        modelBuilder.Entity<DbAggregateRuleExecutionResult>().Property(x => x.RiskLevel).HasMaxLength(TextSizePresets.S64).IsRequired();
        modelBuilder.ApplyJsonFunctions();
        // Risk rules definitions configuration.
        modelBuilder.ApplyConfiguration(new AppSettingMap());
    }
}
