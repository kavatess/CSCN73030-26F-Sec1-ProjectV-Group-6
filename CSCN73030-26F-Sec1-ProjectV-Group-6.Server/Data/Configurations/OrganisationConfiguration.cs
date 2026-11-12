/**
 * @file OrganisationConfiguration.cs
 * @brief EF Core mappings for Department and Service.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Configurations;

/// <summary>EF Core mapping for the Department table: unique Code.</summary>
public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    /// <summary>Configures the Department table.</summary>
    /// <param name="b">Entity builder.</param>
    public void Configure(EntityTypeBuilder<Department> b)
    {
        b.ToTable("Department");
        b.HasKey(d => d.DepartmentId);
        b.Property(d => d.Name).HasMaxLength(100).IsRequired();
        b.Property(d => d.Code).HasMaxLength(10).IsRequired();
        b.HasIndex(d => d.Code).IsUnique();
    }
}

/// <summary>EF Core mapping for the Service table: name unique per department.</summary>
public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    /// <summary>Configures the Service table.</summary>
    /// <param name="b">Entity builder.</param>
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
