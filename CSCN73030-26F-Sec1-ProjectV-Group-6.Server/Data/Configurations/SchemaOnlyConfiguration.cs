using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Configurations;

// Tables created now so next sprint needs no schema migration churn.
// No endpoint in this release reads or writes them.

public class CompensationConfiguration : IEntityTypeConfiguration<Compensation>
{
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

public class SecurityFlagConfiguration : IEntityTypeConfiguration<SecurityFlag>
{
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
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
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

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
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
