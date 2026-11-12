/**
 * @file DepartmentsApiTests.cs
 * @brief Regression tests for /departments, /departments/{code}/services and /departments/headcount.
 */

using System.Net;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>Departments, their services and per-department headcount.</summary>
    public class DepartmentsApiTests(StaffHrApiFactory factory) : ApiTestBase(factory)
    {
        [Fact]
        public async Task List_ReturnsTheSevenDepartmentsSortedByCode()
        {
            var departments = await GetAsync<List<DepartmentDto>>("/departments");

            Assert.Equal(["BUD", "EMS", "HRS", "MNT", "PTR", "RES", "SEC"], departments.Select(d => d.Code));
            Assert.All(departments, d => Assert.Equal(3, d.ServiceCount));
            Assert.Equal("Emergency Services", departments.Single(d => d.Code == "EMS").Name);
        }

        [Fact]
        public async Task Services_ReturnsTheDepartmentsServicesSortedByName()
        {
            var services = await GetAsync<List<ServiceDto>>("/departments/EMS/services");

            Assert.Equal(["Emergency Dispatch", "Fire & Rescue", "Paramedic Service"], services.Select(s => s.Name));
            Assert.All(services, s => Assert.Equal("EMS", s.DepartmentCode));
            Assert.Equal(Seed.ParamedicServiceId, services.Single(s => s.Name == "Paramedic Service").ServiceId);
        }

        [Fact]
        public async Task Services_DepartmentCodeIsCaseInsensitive()
        {
            var services = await GetAsync<List<ServiceDto>>("/departments/ems/services");
            Assert.Equal(3, services.Count);
        }

        [Fact]
        public async Task Services_UnknownDepartment_Returns404Problem()
        {
            var problem = await AssertProblemAsync(await Client.GetAsync("/departments/XYZ/services"), HttpStatusCode.NotFound);
            Assert.Contains("XYZ", problem.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Headcount_CountsActiveOnDutyAndVacanciesFromSeedData()
        {
            var rows = (await GetAsync<List<DepartmentHeadcountDto>>("/departments/headcount")).ToDictionary(r => r.Code);

            Assert.Equal(7, rows.Count);
            // EMS: 8 staff, Rowan is OnLeave → 7 active; Desmond and Priya are on the 07:00–19:00 shift at 12:00;
            // REQ-0000001 is Filling with 1 of 3 filled → 2 vacancies.
            Assert.Equal(new DepartmentHeadcountDto("EMS", "Emergency Services", 7, 2, 2), rows["EMS"]);
            // SEC: Noor is Suspended → 2 active; nobody rostered; REQ-0000003 Approved for 2.
            Assert.Equal(new DepartmentHeadcountDto("SEC", "Security & Policing", 2, 0, 2), rows["SEC"]);
            // PTR: Matteo is Terminated → 2 active; the AwaitingBudget requisition is not a vacancy yet.
            Assert.Equal(new DepartmentHeadcountDto("PTR", "Public Transport", 2, 0, 0), rows["PTR"]);
            // BUD: the Rejected requisition is not a vacancy.
            Assert.Equal(new DepartmentHeadcountDto("BUD", "Budget & Treasury", 1, 0, 0), rows["BUD"]);
            Assert.Equal(new DepartmentHeadcountDto("MNT", "Maintenance", 2, 0, 0), rows["MNT"]);
            Assert.Equal(new DepartmentHeadcountDto("RES", "Residence", 1, 0, 0), rows["RES"]);
        }
    }
}
