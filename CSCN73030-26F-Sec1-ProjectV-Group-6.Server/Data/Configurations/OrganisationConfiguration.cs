using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> b)
    {
        b.ToTable("Department");
        b.HasKey(d => d.DepartmentId);
        b.Property(d => d.Name).HasMaxLength(100).IsRequired();
        b.Property(d => d.Code).HasMaxLength(10).IsRequired();
        b.HasIndex(d => d.Code).IsUnique();
    }
}

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> b)
    {
        b.ToTable("Service");
        b.HasKey(s => s.ServiceId);
        b.Property(s => s.Name).HasMaxLength(100).IsRequired();
        b.HasOne(s => s.Department)
            .WithMany(d => d.Services)
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(s => new { s.DepartmentId, s.Name }).IsUnique();
    }
}
