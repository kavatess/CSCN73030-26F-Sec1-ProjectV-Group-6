/**
 * @file AuthSetupTests.cs
 * @brief Unit tests for the development sign-in safety guard.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Unit
{
    /// <summary>Development mode must never be usable outside the Development environment.</summary>
    public class AuthSetupTests
    {
        private static WebApplicationBuilder Builder(string environment, string mode)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
            builder.Configuration["Authentication:Mode"] = mode;
            return builder;
        }

        [Theory]
        [InlineData("Production")]
        [InlineData("Staging")]
        public void DevelopmentMode_OutsideDevelopment_RefusesToStart(string environment)
        {
            var ex = Assert.Throws<InvalidOperationException>(() => Builder(environment, "Development").AddStaffHrAuth());
            Assert.Contains("Development", ex.Message);
        }

        [Fact]
        public void DevelopmentMode_InDevelopment_IsAllowed()
        {
            Builder("Development", "Development").AddStaffHrAuth();
        }

        [Fact]
        public void EntraIdMode_IsAllowedEverywhere()
        {
            Builder("Production", "EntraId").AddStaffHrAuth();
        }

        [Fact]
        public void DefaultMode_IsEntraId()
        {
            Assert.False(new AuthOptions().IsDevelopmentMode);
        }
    }
}
