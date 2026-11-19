/**
 * @file ObservabilitySetup.cs
 * @brief Key Vault configuration and OpenTelemetry / Application Insights wiring.
 */

using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Metrics;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Observability;

/// <summary>Cloud configuration and telemetry for Program.cs. Both are off unless configured.</summary>
public static class ObservabilitySetup
{
    /// <summary>
    /// When <c>KeyVault:Uri</c> is set, adds Azure Key Vault as a configuration source, authenticating with
    /// the app's managed identity. Secret names use "--" for ":", e.g. ConnectionStrings--StaffHr.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>True when Key Vault was added.</returns>
    public static bool AddKeyVaultIfConfigured(this WebApplicationBuilder builder)
    {
        var uri = builder.Configuration["KeyVault:Uri"];
        if (string.IsNullOrWhiteSpace(uri)) return false;

        builder.Configuration.AddAzureKeyVault(new Uri(uri), new DefaultAzureCredential());
        return true;
    }

    /// <summary>
    /// Registers the business metrics, and when an Application Insights connection string is configured
    /// (<c>APPLICATIONINSIGHTS_CONNECTION_STRING</c> or <c>ApplicationInsights:ConnectionString</c>),
    /// exports traces, metrics (including the CmesStaffHr meter) and logs to Azure Monitor.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>True when Azure Monitor export was enabled.</returns>
    public static bool AddStaffHrTelemetry(this WebApplicationBuilder builder)
    {
        builder.Services.AddMetrics();
        builder.Services.AddSingleton<StaffHrMetrics>();

        var connectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
                               ?? builder.Configuration["ApplicationInsights:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString)) return false;

        builder.Services.AddOpenTelemetry()
            .UseAzureMonitor(options => options.ConnectionString = connectionString)
            .WithMetrics(metrics => metrics.AddMeter(StaffHrMetrics.MeterName));
        return true;
    }
}
