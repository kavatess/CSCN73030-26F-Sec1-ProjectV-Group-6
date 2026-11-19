/**
 * @file SchemaOnlyConfiguration.cs
 * @brief EF Core mappings for the tables not used by this release.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Configurations;

// Tables that support the audit, integration-event, security-flag and payroll features.

/// <summary>EF Core mapping for the Compensation table: salary history, never exposed.</summary>
public class CompensationConfiguration : IEntityTypeConfiguration<Compensation>
{
    /// <summary>Configures the Compensation table.</summary>
    /// <param name="b">Entity builder.</param>
    public void Configure(EntityTypeBuilder<Compensation> b)
    {
        b.ToTable("Compensation", t =>
            t.HasCheckConstraint("CK_Compensation_Dates", "[EffectiveTo] IS NULL OR [EffectiveFrom] < [EffectiveTo]"));
        b.HasKey(c => c.CompensationId);
        b.Property(c => c.PayBand).HasMaxLength(20).IsRequired();
        b.Property(c => c.AnnualSalary).HasPrecision(12, 2);
        b.HasOne(c => c.Staff)
            .WithMany()
            .HasForeignKey(c => c.StaffId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>EF Core mapping for the SecurityFlag table: flags from the Security service.</summary>
public class SecurityFlagConfiguration : IEntityTypeConfiguration<SecurityFlag>
{
    /// <summary>Configures the SecurityFlag table.</summary>
    /// <param name="b">Entity builder.</param>
    public void Configure(EntityTypeBuilder<SecurityFlag> b)
    {
        b.ToTable("SecurityFlag");
        b.HasKey(f => f.FlagId);
        b.Property(f => f.FlagType).HasMaxLength(50).IsRequired();
        b.Property(f => f.Status).HasMaxLength(20).IsRequired();
        b.HasOne(f => f.Staff)
            .WithMany()
            .HasForeignKey(f => f.StaffId)
            .OnDelete(DeleteBehavior.Restrict);
        // One row per flag type per person: the Security service's updates are upserts.
        b.HasIndex(f => new { f.StaffId, f.FlagType }).IsUnique();
    }
}

/// <summary>EF Core mapping for the AuditLog table: audit trail.</summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    /// <summary>Configures the AuditLog table.</summary>
    /// <param name="b">Entity builder.</param>
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLog");
        b.HasKey(a => a.AuditId);
        b.Property(a => a.EntityType).HasMaxLength(50).IsRequired();
        b.Property(a => a.EntityId).HasMaxLength(50).IsRequired();
        b.Property(a => a.Action).HasMaxLength(50).IsRequired();
        b.Property(a => a.ActorId).HasMaxLength(100).IsRequired();
        b.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}

/// <summary>EF Core mapping for the OutboxEvent table: event outbox, indexed on PublishedAtUtc.</summary>
public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    /// <summary>Configures the OutboxEvent table.</summary>
    /// <param name="b">Entity builder.</param>
    public void Configure(EntityTypeBuilder<OutboxEvent> b)
    {
        b.ToTable("OutboxEvent");
        b.HasKey(e => e.EventId);
        b.Property(e => e.EventId).ValueGeneratedNever();
        b.Property(e => e.EventType).HasMaxLength(100).IsRequired();
        b.Property(e => e.PayloadJson).IsRequired();
        // Lets the future publisher find unpublished events quickly.
        b.HasIndex(e => e.PublishedAtUtc);
    }
}
