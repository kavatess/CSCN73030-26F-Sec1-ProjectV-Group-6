/**
 * @file CmesDbContext.cs
 * @brief EF Core database context for the Staff/HR service.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data;

/// <summary>EF Core context for the Staff/HR database. Table mappings live in Data/Configurations.</summary>
public class CmesDbContext : DbContext
{
    /// <summary>Creates the context. Options (the SQL Server connection) come from Program.cs.</summary>
    /// <param name="options">Provider and connection settings.</param>
    public CmesDbContext(DbContextOptions<CmesDbContext> options) : base(options)
    {
    }

    /// <summary>City-wide sequence behind STF-0000000 staff numbers. Values are never reused.</summary>
    public const string StaffNumberSequence = "StaffNumberSequence";

    /// <summary>Sequence behind REQ-0000000 requisition numbers.</summary>
    public const string RequisitionNumberSequence = "RequisitionNumberSequence";

    /// <summary>City departments.</summary>
    public DbSet<Department> Departments => Set<Department>();
    /// <summary>Services inside departments.</summary>
    public DbSet<Service> Services => Set<Service>();
    /// <summary>Employees.</summary>
    public DbSet<Staff> Staff => Set<Staff>();
    /// <summary>Roles held by staff over time.</summary>
    public DbSet<StaffAssignment> StaffAssignments => Set<StaffAssignment>();
    /// <summary>Duty roster shifts.</summary>
    public DbSet<RosterEntry> RosterEntries => Set<RosterEntry>();
    /// <summary>Hire (and later termination) requisitions.</summary>
    public DbSet<Requisition> Requisitions => Set<Requisition>();
    /// <summary>Kinds of licence and certification.</summary>
    public DbSet<CredentialType> CredentialTypes => Set<CredentialType>();
    /// <summary>Licences and certifications held by staff.</summary>
    public DbSet<Credential> Credentials => Set<Credential>();
    /// <summary>Pay history. Schema only; never exposed (business rule 4).</summary>
    public DbSet<Compensation> Compensations => Set<Compensation>();
    /// <summary>Security flags. Schema only.</summary>
    public DbSet<SecurityFlag> SecurityFlags => Set<SecurityFlag>();
    /// <summary>Audit trail. Schema only.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    /// <summary>Integration event outbox. Schema only.</summary>
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    /// <summary>Declares the two number sequences and applies every IEntityTypeConfiguration in Data/Configurations.</summary>
    /// <param name="modelBuilder">EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>(StaffNumberSequence).StartsAt(1).IncrementsBy(1);
        modelBuilder.HasSequence<long>(RequisitionNumberSequence).StartsAt(1).IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CmesDbContext).Assembly);
    }

    /// <summary>Stores enums as strings and marks every DateTime read back from the database as UTC.</summary>
    /// <param name="configurationBuilder">EF Core convention builder.</param>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Store every enum as its name, e.g. 'Active', not 0.
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(20);

        // Every DateTime in this database is UTC. SQL Server's datetime2 does not remember that,
        // so mark values as UTC when they are read back; JSON then serialises them with a 'Z'.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    /// <summary>Converts local times to UTC on write and tags values as UTC on read.</summary>
    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(toDb => toDb.Kind == DateTimeKind.Local ? toDb.ToUniversalTime() : toDb,
                   fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc))
        {
        }
    }
}
