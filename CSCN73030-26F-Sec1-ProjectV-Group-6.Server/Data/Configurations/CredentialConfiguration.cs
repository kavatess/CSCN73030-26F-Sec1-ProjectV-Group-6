/**
 * @file CredentialConfiguration.cs
 * @brief EF Core mappings for CredentialType and Credential.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Configurations;

/// <summary>EF Core mapping for the CredentialType table: unique Name.</summary>
public class CredentialTypeConfiguration : IEntityTypeConfiguration<CredentialType>
{
    /// <summary>Configures the CredentialType table.</summary>
    /// <param name="b">Entity builder.</param>
    public void Configure(EntityTypeBuilder<CredentialType> b)
    {
        b.ToTable("CredentialType");
        b.HasKey(t => t.CredentialTypeId);
        b.Property(t => t.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(t => t.Name).IsUnique();
    }
}

/// <summary>EF Core mapping for the Credential table: check constraint IssueDate &lt; ExpiryDate (rule 6), index on ExpiryDate.</summary>
public class CredentialConfiguration : IEntityTypeConfiguration<Credential>
{
    /// <summary>Configures the Credential table.</summary>
    /// <param name="b">Entity builder.</param>
    public void Configure(EntityTypeBuilder<Credential> b)
    {
        b.ToTable("Credential", t =>
            t.HasCheckConstraint("CK_Credential_Dates", "[IssueDate] < [ExpiryDate]"));
        b.HasKey(c => c.CredentialId);
        b.Property(c => c.Number).HasMaxLength(50).IsRequired();
        b.HasOne(c => c.Staff)
            .WithMany(s => s.Credentials)
            .HasForeignKey(c => c.StaffId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(c => c.CredentialType)
            .WithMany()
            .HasForeignKey(c => c.CredentialTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(c => c.ExpiryDate);
    }
}
