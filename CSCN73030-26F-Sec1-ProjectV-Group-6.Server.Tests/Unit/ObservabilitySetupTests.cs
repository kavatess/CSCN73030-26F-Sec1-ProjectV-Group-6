/**
 * @file ObservabilitySetupTests.cs
 * @brief Unit tests: Key Vault and Azure Monitor are only switched on when configured.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Observability;
using Microsoft.AspNetCore.Builder;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Unit
{
    /// <summary>Local runs need no Azure resources.</summary>
    public class ObservabilitySetupTests
    {
        [Fact]
        public void KeyVault_IsSkippedWithoutAUri()
        {
            Assert.False(WebApplication.CreateBuilder().AddKeyVaultIfConfigured());
        }

        [Fact]
        public void Telemetry_IsLocalOnlyWithoutAConnectionString()
        {
            Assert.False(WebApplication.CreateBuilder().AddStaffHrTelemetry());
        }

        [Fact]
        public void Telemetry_ExportsToAzureMonitorWhenConfigured()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Configuration["ApplicationInsights:ConnectionString"] =
                "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost/";

            Assert.True(builder.AddStaffHrTelemetry());
        }
    }
}
