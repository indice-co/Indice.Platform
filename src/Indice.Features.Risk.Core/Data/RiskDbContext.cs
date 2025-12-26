using System.Globalization;
using Indice.Configuration;
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
    public DbSet<DbRiskEvent> RiskEvents => Set<DbRiskEvent>();

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
        modelBuilder.Entity<DbRiskEvent>().ToTable("RiskEvent");
        modelBuilder.Entity<DbRiskEvent>().HasKey(x => x.Id);
        modelBuilder.Entity<DbRiskEvent>().HasIndex(x => x.SubjectId);
        modelBuilder.Entity<DbRiskEvent>().Property(x => x.Amount).HasColumnType("money");
        modelBuilder.Entity<DbRiskEvent>().Property(x => x.IpAddress).HasMaxLength(TextSizePresets.M128);
        modelBuilder.Entity<DbRiskEvent>().Property(x => x.SubjectId).HasMaxLength(TextSizePresets.M256).IsRequired();
        modelBuilder.Entity<DbRiskEvent>().Property(x => x.Name).HasMaxLength(TextSizePresets.M256);
        modelBuilder.Entity<DbRiskEvent>().Property(x => x.Type).HasMaxLength(TextSizePresets.M256).IsRequired();
        modelBuilder.Entity<DbRiskEvent>().Property(x => x.Data).HasJsonConversion();
        modelBuilder.Entity<DbRiskEvent>().Property(x => x.SourceId).HasMaxLength(TextSizePresets.M256);
        modelBuilder.Entity<DbRiskEvent>().Property(x => x.SourceTransId).HasMaxLength(TextSizePresets.M128);
        modelBuilder.Entity<DbRiskEvent>().Property(x => x.Location).HasMaxLength(TextSizePresets.M256);
        modelBuilder.Entity<DbRiskEvent>().Property(x => x.SessionId).HasMaxLength(TextSizePresets.M128);
        modelBuilder.Entity<DbRiskEvent>()
            .HasIndex(x => x.SessionId)
            .HasDatabaseName("IX_RiskEvent_SessionId")
            .HasFilter("[SessionId] IS NOT NULL");
        // We are using 3 as length because CountryIsoCode may store either ISO 3166-1 alpha-2 country codes (2 characters)
        // or UN M.49 region codes (up to 3 characters, e.g., "419" for Latin America and the Caribbean).
        modelBuilder.Entity<DbRiskEvent>().Property(x => x.CountryIsoCode).HasMaxLength(3);
        modelBuilder.Entity<DbRiskEvent>().Property(x => x.Coordinates);
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
