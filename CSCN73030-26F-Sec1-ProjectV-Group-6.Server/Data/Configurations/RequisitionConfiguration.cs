/**
 * @file RequisitionConfiguration.cs
 * @brief EF Core mapping for Requisition.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Configurations;

/// <summary>EF Core mapping for the Requisition table: unique RequisitionNumber and IdempotencyKey (rule 3), positions check constraints.</summary>
public class RequisitionConfiguration : IEntityTypeConfiguration<Requisition>
{
    /// <summary>Configures the Requisition table.</summary>
    /// <param name="b">Entity builder.</param>
    public void Configure(EntityTypeBuilder<Requisition> b)
    {
        b.ToTable("Requisition", t =>
        {
            t.HasCheckConstraint("CK_Requisition_Positions", "[Positions] > 0");
            t.HasCheckConstraint("CK_Requisition_PositionsFilled",
                "[PositionsFilled] >= 0 AND [PositionsFilled] <= [Positions]");
        });
        b.HasKey(r => r.RequisitionId);

        b.Property(r => r.RequisitionNumber).HasMaxLength(11).IsFixedLength().IsRequired();
        b.HasIndex(r => r.RequisitionNumber).IsUnique();

        b.Property(r => r.IdempotencyKey).HasMaxLength(100).IsRequired();
        b.HasIndex(r => r.IdempotencyKey).IsUnique(); // Business rule 3

        b.Property(r => r.RoleTitle).HasMaxLength(100).IsRequired();
        b.Property(r => r.ReasonCode).HasMaxLength(50);

        b.HasOne(r => r.RequestingDepartment)
            .WithMany()
            .HasForeignKey(r => r.RequestingDeptId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Staff)
            .WithMany()
            .HasForeignKey(r => r.StaffId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
