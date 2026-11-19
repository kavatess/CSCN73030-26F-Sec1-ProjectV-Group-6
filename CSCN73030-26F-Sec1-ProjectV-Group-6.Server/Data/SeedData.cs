using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data;

/// <summary>Fictional starter data. Contains no real people or personal information.</summary>
public static class SeedData
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>().HasData(
            new Department { DepartmentId = 1, Name = "Staff/HR", Code = "HR" },
            new Department { DepartmentId = 2, Name = "Residence", Code = "RES" },
            new Department { DepartmentId = 3, Name = "Security", Code = "SEC" },
            new Department { DepartmentId = 4, Name = "Public Transport", Code = "TRN" },
            new Department { DepartmentId = 5, Name = "Maintenance", Code = "MNT" },
            new Department { DepartmentId = 6, Name = "Budget", Code = "BUD" },
            new Department { DepartmentId = 7, Name = "Emergency Services", Code = "EMG" });

        modelBuilder.Entity<CredentialType>().HasData(
            new CredentialType { CredentialTypeId = 1, Name = "Paramedic Certification" },
            new CredentialType { CredentialTypeId = 2, Name = "Heavy Machinery Operator Licence" },
            new CredentialType { CredentialTypeId = 3, Name = "Bus Driver Licence" });

        modelBuilder.Entity<Staff>().HasData(
            new Staff { StaffId = 1, StaffNumber = "EMP1001", FirstName = "Alex", LastName = "Example", Email = "alex.example@example.com", HireDate = new DateOnly(2024, 3, 4), EmploymentStatus = EmploymentStatus.Active },
            new Staff { StaffId = 2, StaffNumber = "EMP1002", FirstName = "Sam", LastName = "Sample", Email = "sam.sample@example.com", HireDate = new DateOnly(2023, 9, 18), EmploymentStatus = EmploymentStatus.Active },
            new Staff { StaffId = 3, StaffNumber = "EMP1003", FirstName = "Jordan", LastName = "Placeholder", Email = "jordan.placeholder@example.com", HireDate = new DateOnly(2022, 1, 10), EmploymentStatus = EmploymentStatus.Terminated });

        modelBuilder.Entity<Credential>().HasData(
            new Credential { CredentialId = 1, StaffId = 1, CredentialTypeId = 1, IssueDate = new DateOnly(2024, 3, 4), ExpiryDate = new DateOnly(2027, 3, 3), Status = CredentialStatus.Valid });
    }
}
