using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Seeding;

/// <summary>
/// Applies migrations, then loads demo data so every HR screen has something to show.
/// Run with: <c>dotnet run --project CSCN73030-26F-Sec1-ProjectV-Group-6.Server -- --seed</c>
/// </summary>
/// <remarks>
/// ALL PEOPLE IN THIS FILE ARE FICTIONAL. Names are invented, emails use the reserved
/// <c>.example</c> domain, phone numbers use the fictional 555-01xx range, and citizen ids are made up.
/// Dates are relative to the day you seed, so credentials are always in a realistic mix of
/// valid / expiring / expired states and the roster always covers "now".
/// The seeder does nothing if departments already exist.
/// </remarks>
public class DatabaseSeeder(
    CmesDbContext db,
    StaffRepository staffRepository,
    RequisitionRepository requisitionRepository,
    Clock clock,
    ILogger<DatabaseSeeder> logger)
{
    private const string EmailDomain = "cmes.example";

    public async Task MigrateAndSeedAsync()
    {
        logger.LogInformation("Applying migrations...");
        await db.Database.MigrateAsync();

        if (await db.Departments.AnyAsync())
        {
            logger.LogInformation("Database already has data; seeding skipped.");
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        var services = SeedDepartmentsAndServices();
        var types = SeedCredentialTypes();
        await db.SaveChangesAsync();

        var staff = await SeedStaffAsync(services);
        await db.SaveChangesAsync();

        SeedCredentials(staff, types);
        SeedParamedicRoster(staff, services["Paramedic Service"]);
        await SeedRequisitionsAsync();
        await db.SaveChangesAsync();

        await transaction.CommitAsync();
        logger.LogInformation("Seeded {Departments} departments, {Staff} staff, {Roster} roster entries, " +
                              "{Requisitions} requisitions and {Credentials} credentials.",
            await db.Departments.CountAsync(), await db.Staff.CountAsync(), await db.RosterEntries.CountAsync(),
            await db.Requisitions.CountAsync(), await db.Credentials.CountAsync());
    }

    // ---- Departments and services ----------------------------------------------------------

    private Dictionary<string, Service> SeedDepartmentsAndServices()
    {
        var departments = new (string Code, string Name, string[] Services)[]
        {
            ("EMS", "Emergency Services", ["Paramedic Service", "Fire & Rescue", "Emergency Dispatch"]),
            ("SEC", "Security & Policing", ["Patrol Division", "Investigations", "Bylaw Enforcement"]),
            ("PTR", "Public Transport", ["Bus Operations", "Light Rail Operations", "Transit Fleet Maintenance"]),
            ("MNT", "Maintenance", ["Roads & Sidewalks", "Water & Sewer", "Parks & Facilities"]),
            ("BUD", "Budget & Treasury", ["Treasury Operations", "Payroll", "Procurement"]),
            ("RES", "Residence", ["Resident Registry", "Housing Services", "Permits & Licensing"]),
            ("HRS", "Human Resources", ["Recruitment", "Employee Relations", "Training & Development"]),
        };

        var byName = new Dictionary<string, Service>();
        foreach (var (code, name, serviceNames) in departments)
        {
            var department = new Department { Code = code, Name = name };
            foreach (var serviceName in serviceNames)
            {
                var service = new Service { Name = serviceName, Department = department };
                department.Services.Add(service);
                byName[serviceName] = service;
            }
            db.Departments.Add(department);
        }
        return byName;
    }

    private Dictionary<string, CredentialType> SeedCredentialTypes()
    {
        var types = new (string Name, bool IsDutyBlocking)[]
        {
            ("Primary Care Paramedic Licence", true),
            ("Advanced Care Paramedic Licence", true),
            ("CPR & First Aid Certificate", true),
            ("Emergency Vehicle Operator Certificate", true),
            ("Emergency Dispatcher Certification", true),
            ("Firefighter Level II Certification", true),
            ("Police Officer Certification", true),
            ("Commercial Driver's Licence - Class B", true),
            ("Water Distribution Operator Certificate", true),
            ("Confined Space Entry Training", false),
            ("Chartered Professional Accountant", false),
            ("Certified HR Professional", false),
        };

        var byName = new Dictionary<string, CredentialType>();
        foreach (var (name, blocking) in types)
        {
            var type = new CredentialType { Name = name, IsDutyBlocking = blocking };
            db.CredentialTypes.Add(type);
            byName[name] = type;
        }
        return byName;
    }

    // ---- Staff and assignments -------------------------------------------------------------

    private record SeedPerson(
        string Key, string First, string Last, string? Preferred, string Service, string Role,
        EmploymentStatus Status, EmploymentType Type, int HiredMonthsAgo, int BirthYear);

    private static readonly SeedPerson[] People =
    [
        // Emergency Services (EMS)
        new("talia", "Talia", "Brennwood", null, "Paramedic Service", "Paramedic Supervisor", EmploymentStatus.Active, EmploymentType.FullTime, 140, 1981),
        new("desmond", "Desmond", "Achterberg", "Des", "Paramedic Service", "Advanced Care Paramedic", EmploymentStatus.Active, EmploymentType.FullTime, 96, 1987),
        new("priya", "Priya", "Valcourt", null, "Paramedic Service", "Primary Care Paramedic", EmploymentStatus.Active, EmploymentType.FullTime, 52, 1992),
        new("jonah", "Jonah", "Ferriday", "Joe", "Paramedic Service", "Primary Care Paramedic", EmploymentStatus.Active, EmploymentType.FullTime, 30, 1994),
        new("ines", "Ines", "Halvorsen", null, "Paramedic Service", "Primary Care Paramedic", EmploymentStatus.Probation, EmploymentType.FullTime, 3, 1999),
        new("kofi", "Kofi", "Marchetti", null, "Paramedic Service", "Advanced Care Paramedic", EmploymentStatus.Active, EmploymentType.FullTime, 75, 1985),
        new("wren", "Wren", "Castellanos", null, "Emergency Dispatch", "Emergency Dispatcher", EmploymentStatus.Active, EmploymentType.PartTime, 40, 1990),
        new("rowan", "Rowan", "Tibbets", null, "Fire & Rescue", "Firefighter", EmploymentStatus.OnLeave, EmploymentType.FullTime, 110, 1983),
        // Security & Policing (SEC)
        new("celeste", "Celeste", "Varga", null, "Patrol Division", "Patrol Constable", EmploymentStatus.Active, EmploymentType.FullTime, 60, 1991),
        new("anders", "Anders", "Kittredge", null, "Investigations", "Detective Sergeant", EmploymentStatus.Active, EmploymentType.FullTime, 180, 1978),
        new("noor", "Noor", "Abernathy", null, "Bylaw Enforcement", "Bylaw Officer", EmploymentStatus.Suspended, EmploymentType.FullTime, 28, 1993),
        // Public Transport (PTR)
        new("lucian", "Lucian", "Okafor", null, "Bus Operations", "Bus Operator", EmploymentStatus.Active, EmploymentType.FullTime, 85, 1986),
        new("greta", "Greta", "Pikeworth", null, "Light Rail Operations", "Light Rail Operator", EmploymentStatus.Active, EmploymentType.FullTime, 46, 1989),
        new("matteo", "Matteo", "Rasmussen", null, "Transit Fleet Maintenance", "Fleet Mechanic", EmploymentStatus.Terminated, EmploymentType.FullTime, 70, 1984),
        // Maintenance (MNT)
        new("saoirse", "Saoirse", "Delacroix", null, "Water & Sewer", "Water Systems Operator", EmploymentStatus.Active, EmploymentType.FullTime, 64, 1988),
        new("bram", "Bram", "Oyelaran", null, "Roads & Sidewalks", "Road Crew Lead", EmploymentStatus.Active, EmploymentType.Seasonal, 20, 1990),
        // Budget & Treasury (BUD)
        new("hollis", "Hollis", "Fairweather", null, "Treasury Operations", "Senior Financial Analyst", EmploymentStatus.Active, EmploymentType.FullTime, 120, 1980),
        // Residence (RES)
        new("ottilie", "Ottilie", "Banerjee", "Tilly", "Housing Services", "Housing Officer", EmploymentStatus.Active, EmploymentType.PartTime, 34, 1995),
        // Human Resources (HRS)
        new("ezra", "Ezra", "Lindqvist", null, "Recruitment", "HR Business Partner", EmploymentStatus.Active, EmploymentType.FullTime, 90, 1984),
        new("marisol", "Marisol", "Tennant", null, "Training & Development", "Training Coordinator", EmploymentStatus.Probation, EmploymentType.Contract, 2, 1996),
    ];

    private async Task<Dictionary<string, Staff>> SeedStaffAsync(Dictionary<string, Service> services)
    {
        var today = clock.Today;
        var byKey = new Dictionary<string, Staff>();

        for (var i = 0; i < People.Length; i++)
        {
            var p = People[i];
            var service = services[p.Service];
            var hireDate = today.AddMonths(-p.HiredMonthsAgo);

            var staff = new Staff
            {
                StaffNumber = await staffRepository.NextStaffNumberAsync(),
                FirstName = p.First,
                LastName = p.Last,
                PreferredName = p.Preferred,
                Email = $"{p.First}.{p.Last}@{EmailDomain}".ToLowerInvariant(),
                DateOfBirth = new DateOnly(p.BirthYear, i % 12 + 1, i * 3 % 27 + 1),
                Phone = $"555-01{i:D2}",
                CitizenId = $"CIT-FAKE-{1000 + i * 37:D6}",
                HireDate = hireDate,
                EmploymentStatus = p.Status,
                EmploymentType = p.Type,
                HomeDepartment = service.Department,
            };

            var assignment = new StaffAssignment { Service = service, RoleTitle = p.Role, StartDate = hireDate };
            if (p.Status == EmploymentStatus.Terminated)
                assignment.EndDate = today.AddMonths(-2);
            staff.Assignments.Add(assignment);

            db.Staff.Add(staff);
            byKey[p.Key] = staff;
        }

        // A bit of history: Desmond was promoted from PCP to ACP two years ago.
        var desmond = byKey["desmond"];
        var promotedOn = today.AddYears(-2);
        desmond.Assignments[0].StartDate = promotedOn;
        desmond.Assignments.Add(new StaffAssignment
        {
            Service = services["Paramedic Service"],
            RoleTitle = "Primary Care Paramedic",
            StartDate = desmond.HireDate,
            EndDate = promotedOn.AddDays(-1),
        });

        // Ezra also sits on the Employee Relations team part of the week.
        byKey["ezra"].Assignments.Add(new StaffAssignment
        {
            Service = services["Employee Relations"],
            RoleTitle = "Employee Relations Advisor",
            StartDate = today.AddMonths(-6),
        });

        return byKey;
    }

    // ---- Credentials -----------------------------------------------------------------------

    private void SeedCredentials(Dictionary<string, Staff> staff, Dictionary<string, CredentialType> types)
    {
        var today = clock.Today;
        var serial = 1000;

        // expiresInDays: negative = already expired; 0-30 = expiring; above 30 = valid.
        void Add(string person, string type, int expiresInDays, bool suspended = false)
        {
            var expiry = today.AddDays(expiresInDays);
            var credential = new Credential
            {
                Staff = staff[person],
                CredentialType = types[type],
                Number = $"FAKE-{serial++}",
                IssueDate = expiry.AddYears(-2),
                ExpiryDate = expiry,
            };
            credential.Status = suspended
                ? CredentialStatus.Suspended
                : CredentialStatusRules.Derive(CredentialStatus.Valid, expiry, today);
            db.Credentials.Add(credential);
        }

        Add("talia", "Advanced Care Paramedic Licence", 410);
        Add("talia", "CPR & First Aid Certificate", 200);
        Add("desmond", "Advanced Care Paramedic Licence", 300);
        Add("desmond", "CPR & First Aid Certificate", 12);             // expiring
        Add("priya", "Primary Care Paramedic Licence", 520);
        Add("priya", "CPR & First Aid Certificate", -5);               // expired
        Add("priya", "Emergency Vehicle Operator Certificate", 260);
        Add("jonah", "Primary Care Paramedic Licence", 21);            // expiring
        Add("jonah", "CPR & First Aid Certificate", 330);
        Add("ines", "Primary Care Paramedic Licence", 700);
        Add("ines", "CPR & First Aid Certificate", 690);
        Add("kofi", "Advanced Care Paramedic Licence", -20);           // expired
        Add("kofi", "CPR & First Aid Certificate", 150);
        Add("wren", "Emergency Dispatcher Certification", 7);          // expiring
        Add("rowan", "Firefighter Level II Certification", 380);
        Add("celeste", "Police Officer Certification", 600);
        Add("anders", "Police Officer Certification", 240);
        Add("noor", "CPR & First Aid Certificate", 90, suspended: true);
        Add("lucian", "Commercial Driver's Licence - Class B", 28);    // expiring
        Add("greta", "Commercial Driver's Licence - Class B", 450);
        Add("matteo", "Commercial Driver's Licence - Class B", -60);   // expired
        Add("saoirse", "Water Distribution Operator Certificate", 365);
        Add("saoirse", "Confined Space Entry Training", -2);           // expired
        Add("bram", "Confined Space Entry Training", 180);
        Add("hollis", "Chartered Professional Accountant", 270);
        Add("ezra", "Certified HR Professional", 500);
        Add("marisol", "Certified HR Professional", 15);               // expiring
    }

    // ---- Roster ----------------------------------------------------------------------------

    /// <summary>
    /// A week of 12-hour shifts for the Paramedic Service, from yesterday to five days ahead, so
    /// someone is always on duty "now". Three crews rotate day shift, then night shift the next
    /// day, then a day off, so nobody works back-to-back shifts.
    /// </summary>
    private void SeedParamedicRoster(Dictionary<string, Staff> staff, Service paramedics)
    {
        string[][] crews = [["desmond", "priya"], ["jonah", "kofi"], ["talia", "ines"]];
        var now = clock.UtcNow;
        var today = clock.Today;

        for (var offset = -1; offset <= 5; offset++)
        {
            var date = today.AddDays(offset);
            var dayCrew = crews[Mod(offset, 3)];
            var nightCrew = crews[Mod(offset + 2, 3)];
            var dayStart = date.ToDateTime(new TimeOnly(7, 0), DateTimeKind.Utc);
            var nightStart = date.ToDateTime(new TimeOnly(19, 0), DateTimeKind.Utc);

            foreach (var person in dayCrew)
                AddShift(staff[person], dayStart, ShiftType.Day);
            foreach (var person in nightCrew)
                AddShift(staff[person], nightStart, ShiftType.Night);
        }

        void AddShift(Staff person, DateTime start, ShiftType type)
        {
            var end = start.AddHours(12);
            var status = end <= now ? RosterStatus.Completed
                : start <= now.AddHours(48) ? RosterStatus.Confirmed
                : RosterStatus.Scheduled;

            // One cancelled shift so the UI can show that state.
            if (person.FirstName == "Kofi" && start.Date == today.AddDays(3).ToDateTime(TimeOnly.MinValue).Date)
                status = RosterStatus.Cancelled;

            db.RosterEntries.Add(new RosterEntry
            {
                Staff = person,
                Service = paramedics,
                ShiftStart = start,
                ShiftEnd = end,
                ShiftType = type,
                Status = status,
            });
        }
    }

    private static int Mod(int value, int modulus) => (value % modulus + modulus) % modulus;

    // ---- Requisitions ----------------------------------------------------------------------

    private async Task SeedRequisitionsAsync()
    {
        var departments = await db.Departments.ToDictionaryAsync(d => d.Code);
        var now = clock.UtcNow;

        var requisitions = new (string Dept, string Role, int Positions, int Filled, RequisitionStatus Status,
            BudgetDecision? Budget, string Reason, int DaysAgo)[]
        {
            ("EMS", "Primary Care Paramedic", 3, 1, RequisitionStatus.Filling, BudgetDecision.Approved, "GROWTH", 75),
            ("SEC", "Patrol Constable", 2, 0, RequisitionStatus.Approved, BudgetDecision.Approved, "ATTRITION", 40),
            ("PTR", "Bus Operator", 4, 0, RequisitionStatus.AwaitingBudget, BudgetDecision.Pending, "SERVICE_EXPANSION", 21),
            ("MNT", "Water Systems Operator", 1, 0, RequisitionStatus.HRReview, null, "REPLACEMENT", 9),
            ("RES", "Housing Officer", 1, 0, RequisitionStatus.Received, null, "NEW_PROGRAM", 2),
            ("BUD", "Payroll Clerk", 1, 0, RequisitionStatus.Rejected, BudgetDecision.Rejected, "BACKFILL", 55),
        };

        var index = 1;
        foreach (var r in requisitions.OrderByDescending(r => r.DaysAgo))
        {
            db.Requisitions.Add(new Requisition
            {
                RequisitionNumber = await requisitionRepository.NextRequisitionNumberAsync(),
                RequestingDepartment = departments[r.Dept],
                Type = RequisitionType.Hire,
                RoleTitle = r.Role,
                Positions = r.Positions,
                PositionsFilled = r.Filled,
                Status = r.Status,
                BudgetDecision = r.Budget,
                ReasonCode = r.Reason,
                IdempotencyKey = $"seed-requisition-{index++:D3}",
                CreatedAtUtc = now.AddDays(-r.DaysAgo),
            });
        }
    }
}
