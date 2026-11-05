using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data;

/// <summary>EF Core context for the Staff/HR database. Table mappings live in Data/Configurations.</summary>
public class CmesDbContext(DbContextOptions<CmesDbContext> options) : DbContext(options)
{
    /// <summary>City-wide sequence behind STF-0000000 staff numbers. Values are never reused.</summary>
    public const string StaffNumberSequence = "StaffNumberSequence";

    /// <summary>Sequence behind REQ-0000000 requisition numbers.</summary>
    public const string RequisitionNumberSequence = "RequisitionNumberSequence";

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<StaffAssignment> StaffAssignments => Set<StaffAssignment>();
    public DbSet<RosterEntry> RosterEntries => Set<RosterEntry>();
    public DbSet<Requisition> Requisitions => Set<Requisition>();
    public DbSet<CredentialType> CredentialTypes => Set<CredentialType>();
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<Compensation> Compensations => Set<Compensation>();
    public DbSet<SecurityFlag> SecurityFlags => Set<SecurityFlag>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>(StaffNumberSequence).StartsAt(1).IncrementsBy(1);
        modelBuilder.HasSequence<long>(RequisitionNumberSequence).StartsAt(1).IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CmesDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Store every enum as its name, e.g. 'Active', not 0.
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(20);

        // Every DateTime in this database is UTC. SQL Server's datetime2 does not remember that,
        // so mark values as UTC when they are read back; JSON then serialises them with a 'Z'.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        toDb => toDb.Kind == DateTimeKind.Local ? toDb.ToUniversalTime() : toDb,
        fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc));
}
