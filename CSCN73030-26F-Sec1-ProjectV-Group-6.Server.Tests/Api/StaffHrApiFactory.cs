/**
 * @file StaffHrApiFactory.cs
 * @brief Boots the real API against a fresh, seeded SQL Server database for the API regression tests.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Events;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.MsSql;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>
    /// Runs the whole application in memory against its own SQL Server database, migrated and
    /// seeded with the demo data, with the clock pinned to <see cref="Now"/>.
    /// </summary>
    /// <remarks>
    /// Where the database comes from:
    /// - If the environment variable <c>STAFFHR_TEST_SQL</c> holds a SQL Server connection string
    ///   (for example LocalDB on Windows), a uniquely named database is created on that server.
    /// - Otherwise a SQL Server 2022 container is started with Testcontainers, which needs Docker.
    /// The database is deleted again when the test run ends.
    /// </remarks>
    public sealed class StaffHrApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        /// <summary>The instant every API test runs at: Thursday 5 November 2026, 12:00 UTC.</summary>
        public static readonly DateTimeOffset Now = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

        /// <summary>Today's date as the API sees it.</summary>
        public static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

        /// <summary>Environment variable naming an existing SQL Server to use instead of Docker.</summary>
        public const string ExternalServerVariable = "STAFFHR_TEST_SQL";

        private MsSqlContainer? _container;
        private string _connectionString = "";

        /// <summary>Captures every integration event the app publishes, in place of Service Bus.</summary>
        public RecordingEventPublisher Events { get; } = new();

        /// <summary>The fake clock injected in place of <see cref="TimeProvider.System"/>.</summary>
        public FakeTimeProvider Time { get; } = new(Now);

        /// <summary>Creates the database, applies the migration and seeds the demo data.</summary>
        public async Task InitializeAsync()
        {
            var server = Environment.GetEnvironmentVariable(ExternalServerVariable);
            if (string.IsNullOrWhiteSpace(server))
            {
                _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
                await _container.StartAsync();
                server = _container.GetConnectionString();
            }

            _connectionString = new SqlConnectionStringBuilder(server)
            {
                InitialCatalog = $"CmesStaffHrTests_{Guid.NewGuid():N}",
                TrustServerCertificate = true,
                MultipleActiveResultSets = true,
            }.ConnectionString;

            using var scope = Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().MigrateAndSeedAsync();
        }

        /// <summary>Deletes the test database and stops the container, if one was started.</summary>
        async Task IAsyncLifetime.DisposeAsync()
        {
            if (_connectionString != "")
            {
                using var scope = Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<CmesDbContext>().Database.EnsureDeletedAsync();
            }
            await DisposeAsync();
            if (_container != null) await _container.DisposeAsync();
        }

        /// <summary>Points the app at the test database and swaps in the fake clock.</summary>
        /// <param name="builder">Web host builder.</param>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:StaffHr", _connectionString);
            // Tests run the outbox dispatcher themselves, so the background loop stays off.
            builder.UseSetting("Messaging:BackgroundPublishing", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Time);
                services.RemoveAll<IEventPublisher>();
                services.AddSingleton<IEventPublisher>(Events);
            });
        }
    }

    /// <summary>
    /// Every API test class joins this collection, so they share one database and run one after
    /// another. Tests that write create their own records (in the HRS department) rather than
    /// changing seeded ones, so the seeded numbers other tests assert on stay true.
    /// </summary>
    [CollectionDefinition(Name)]
    public class StaffHrApiCollection : ICollectionFixture<StaffHrApiFactory>
    {
        /// <summary>Collection name used in <c>[Collection(...)]</c>.</summary>
        public const string Name = "Staff/HR API";
    }
}
