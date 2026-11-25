/**
 * @file AppDbContext.cs
 * @brief Draft DbContext for the Staff/HR schema (HR-02).
 *
 * Part of the HR-02 schema draft (PR #3). Not registered in Program.cs and not used at runtime: the service uses CmesDbContext and the Domain entities. Its own migration lives in Migrations/; every dotnet ef command names --context CmesDbContext.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data;

/// <summary>The Staff/HR database: the tables and the rules that go with them.</summary>
public class AppDbContext : DbContext
{
    /// <summary>Creates the context with the given options.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>The Department table.</summary>
    public DbSet<Department> Departments => Set<Department>();
    /// <summary>The Service table.</summary>
    public DbSet<Service> Services => Set<Service>();
    /// <summary>The Staff table.</summary>
    public DbSet<Staff> StaffMembers => Set<Staff>();
    /// <summary>The StaffAssignment table.</summary>
    public DbSet<StaffAssignment> StaffAssignments => Set<StaffAssignment>();
    /// <summary>The RosterEntry table.</summary>
    public DbSet<RosterEntry> RosterEntries => Set<RosterEntry>();
    /// <summary>The Requisition table.</summary>
    public DbSet<Requisition> Requisitions => Set<Requisition>();
    /// <summary>The CredentialType table.</summary>
    public DbSet<CredentialType> CredentialTypes => Set<CredentialType>();
    /// <summary>The Credential table.</summary>
    public DbSet<Credential> Credentials => Set<Credential>();
    /// <summary>The SecurityFlag table.</summary>
    public DbSet<SecurityFlag> SecurityFlags => Set<SecurityFlag>();
    /// <summary>The Compensation table.</summary>
    public DbSet<Compensation> Compensations => Set<Compensation>();
    /// <summary>The AuditLog table.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    /// <summary>The OutboxEvent table.</summary>
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    /// <summary>Stores enums as text and money as decimal(12,2).</summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Store enums as readable text ("Active") instead of numbers (0).
        configurationBuilder.Properties<EmploymentStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<RequisitionType>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<RequisitionStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<BudgetDecisionResult>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<CredentialStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<SecurityFlagStatus>().HaveConversion<string>().HaveMaxLength(20);

        // Money: 12 digits in total, 2 after the decimal point.
        configurationBuilder.Properties<decimal>().HavePrecision(12, 2);
    }

    /// <summary>Keys, sizes, unique indexes and check constraints of the HR-02 tables.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(e =>
        {
            e.Property(d => d.Name).HasMaxLength(100);
            e.Property(d => d.Code).HasMaxLength(10);
            e.HasIndex(d => d.Name).IsUnique();
            e.HasIndex(d => d.Code).IsUnique();
        });

        modelBuilder.Entity<Service>(e =>
        {
            e.Property(s => s.Name).HasMaxLength(100);
            e.HasIndex(s => new { s.DepartmentId, s.Name }).IsUnique();
        });

        modelBuilder.Entity<Staff>(e =>
        {
            e.Property(s => s.StaffNumber).HasMaxLength(20);
            e.Property(s => s.FirstName).HasMaxLength(100);
            e.Property(s => s.LastName).HasMaxLength(100);
            e.Property(s => s.Email).HasMaxLength(254);
            e.HasIndex(s => s.StaffNumber).IsUnique();
        });

        modelBuilder.Entity<StaffAssignment>(e =>
        {
            e.Property(a => a.RoleTitle).HasMaxLength(100);
            // One current assignment per person: only one row per StaffId may have no EndDate.
            e.HasIndex(a => a.StaffId).IsUnique().HasFilter("[EndDate] IS NULL");
        });

        modelBuilder.Entity<RosterEntry>(e =>
        {
            // A shift must end after it starts.
            e.ToTable(t => t.HasCheckConstraint("CK_RosterEntry_ShiftTimes", "[ShiftEnd] > [ShiftStart]"));
            e.HasIndex(r => new { r.StaffId, r.ShiftStart });
        });

        modelBuilder.Entity<Requisition>(e =>
        {
            e.Property(r => r.ReasonCode).HasMaxLength(50);
            e.Property(r => r.RoleTitle).HasMaxLength(100);
            e.Property(r => r.IdempotencyKey).HasMaxLength(100);
            // A retried request with the same key must not create a second record.
            e.HasIndex(r => r.IdempotencyKey).IsUnique();
            // The property is called RequestingDeptId, so EF needs to be told which key it is.
            e.HasOne(r => r.RequestingDepartment).WithMany().HasForeignKey(r => r.RequestingDeptId);
        });

        modelBuilder.Entity<CredentialType>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(100);
            e.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<SecurityFlag>(e => e.Property(f => f.FlagType).HasMaxLength(50));

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(a => a.EntityType).HasMaxLength(100);
            e.Property(a => a.EntityId).HasMaxLength(100);
            e.Property(a => a.Action).HasMaxLength(50);
            e.Property(a => a.ActorId).HasMaxLength(100);
        });

        modelBuilder.Entity<OutboxEvent>(e => e.Property(o => o.EventType).HasMaxLength(100));

        SeedData.Apply(modelBuilder);
        
        // Never cascade deletes: removing a parent row must fail, not silently delete history.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
