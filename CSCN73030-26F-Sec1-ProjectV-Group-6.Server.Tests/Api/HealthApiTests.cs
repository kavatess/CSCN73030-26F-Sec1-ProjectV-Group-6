/**
 * @file HealthApiTests.cs
 * @brief Regression tests for GET /health.
 */

using System.Net;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>GET /health reports the service and its database.</summary>
    public class HealthApiTests(StaffHrApiFactory factory) : ApiTestBase(factory)
    {
        [Fact]
        public async Task Health_WithDatabaseUp_Returns200Healthy()
        {
            var health = await ReadAsync<HealthDto>(await Client.GetAsync("/health"), HttpStatusCode.OK);

            Assert.Equal("Healthy", health.Status);
            Assert.Equal("Healthy", health.Checks["database"]);
        }
    }
}
