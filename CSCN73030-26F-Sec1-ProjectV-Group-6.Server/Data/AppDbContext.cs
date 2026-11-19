using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data;

/// <summary>The Staff/HR database: the tables and the rules that go with them.</summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Staff> StaffMembers => Set<Staff>();
    public DbSet<StaffAssignment> StaffAssignments => Set<StaffAssignment>();
    public DbSet<RosterEntry> RosterEntries => Set<RosterEntry>();
    public DbSet<Requisition> Requisitions => Set<Requisition>();
    public DbSet<CredentialType> CredentialTypes => Set<CredentialType>();
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<SecurityFlag> SecurityFlags => Set<SecurityFlag>();
    public DbSet<Compensation> Compensations => Set<Compensation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

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
