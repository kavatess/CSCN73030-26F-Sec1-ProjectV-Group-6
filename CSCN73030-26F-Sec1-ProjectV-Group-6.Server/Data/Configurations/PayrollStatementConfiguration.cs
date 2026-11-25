/**
 * @file PayrollStatementConfiguration.cs
 * @brief EF Core mapping for PayrollStatement.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Configurations;

/// <summary>Table PayrollStatement: one row per department and month.</summary>
public class PayrollStatementConfiguration : IEntityTypeConfiguration<PayrollStatement>
{
    /// <summary>Keys, sizes and the one-per-department-and-month rule.</summary>
    public void Configure(EntityTypeBuilder<PayrollStatement> b)
    {
        b.ToTable("PayrollStatement", t => t.HasCheckConstraint("CK_PayrollStatement_Amount", "[Amount] >= 0"));
        b.HasKey(p => p.PayrollStatementId);
        b.Property(p => p.Period).HasMaxLength(7).IsFixedLength().IsRequired();
        b.HasIndex(p => new { p.Period, p.DepartmentId }).IsUnique();
        b.Property(p => p.Amount).HasPrecision(18, 2);
        b.Property(p => p.RemainingBudget).HasPrecision(18, 2);
        b.Property(p => p.Message).HasMaxLength(500);
        b.Property(p => p.SentBy).HasMaxLength(256).IsRequired();
        b.HasOne(p => p.Department).WithMany().HasForeignKey(p => p.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
