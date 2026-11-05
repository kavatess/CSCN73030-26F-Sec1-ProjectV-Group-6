using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Configurations;

public class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> b)
    {
        b.ToTable("Staff");
        b.HasKey(s => s.StaffId);

        b.Property(s => s.StaffNumber).HasMaxLength(StaffNumber.Length).IsFixedLength().IsRequired();
        b.HasIndex(s => s.StaffNumber).IsUnique();
        // Business rule 1: the staff number is immutable. EF throws if anything tries to
        // update it after the row is first saved, as a backstop to the API-level check.
        b.Property(s => s.StaffNumber).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        b.Property(s => s.FirstName).HasMaxLength(100).IsRequired();
        b.Property(s => s.LastName).HasMaxLength(100).IsRequired();
        b.Property(s => s.PreferredName).HasMaxLength(100);

        b.Property(s => s.Email).HasMaxLength(254).IsRequired();
        b.HasIndex(s => s.Email).IsUnique(); // Business rule 2

        b.Property(s => s.Phone).HasMaxLength(30);
        b.Property(s => s.CitizenId).HasMaxLength(30);

        b.HasOne(s => s.HomeDepartment)
            .WithMany()
            .HasForeignKey(s => s.HomeDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(s => new { s.LastName, s.FirstName });
        b.Ignore(s => s.DisplayName);
    }
}

public class StaffAssignmentConfiguration : IEntityTypeConfiguration<StaffAssignment>
{
    public void Configure(EntityTypeBuilder<StaffAssignment> b)
    {
        b.ToTable("StaffAssignment", t =>
            t.HasCheckConstraint("CK_StaffAssignment_Dates", "[EndDate] IS NULL OR [StartDate] < [EndDate]"));
        b.HasKey(a => a.AssignmentId);
        b.Property(a => a.RoleTitle).HasMaxLength(100).IsRequired();
        b.HasOne(a => a.Staff)
            .WithMany(s => s.Assignments)
            .HasForeignKey(a => a.StaffId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.Service)
            .WithMany()
            .HasForeignKey(a => a.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RosterEntryConfiguration : IEntityTypeConfiguration<RosterEntry>
{
    public void Configure(EntityTypeBuilder<RosterEntry> b)
    {
        b.ToTable("RosterEntry", t =>
            t.HasCheckConstraint("CK_RosterEntry_Shift", "[ShiftStart] < [ShiftEnd]"));
        b.HasKey(r => r.RosterEntryId);
        b.HasOne(r => r.Staff)
            .WithMany()
            .HasForeignKey(r => r.StaffId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Service)
            .WithMany()
            .HasForeignKey(r => r.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
        // Supports both the overlap check (by staff) and the roster/on-duty queries (by service).
        b.HasIndex(r => new { r.StaffId, r.ShiftStart });
        b.HasIndex(r => new { r.ServiceId, r.ShiftStart });
    }
}
