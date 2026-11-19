/**
 * @file ApiTestBase.cs
 * @brief Shared HTTP helpers, JSON settings and seed-data facts for the API regression tests.
 */

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Events;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>Facts about the seeded demo data that the tests rely on (see DatabaseSeeder).</summary>
    public static class Seed
    {
        /// <summary>Talia Brennwood, Paramedic Supervisor (EMS).</summary>
        public const string Talia = "STF-0000001";
        /// <summary>Desmond "Des" Achterberg, Advanced Care Paramedic (EMS); on the day shift at 12:00 on the test day.</summary>
        public const string Desmond = "STF-0000002";
        /// <summary>Priya Valcourt, Primary Care Paramedic (EMS); on the day shift at 12:00 on the test day.</summary>
        public const string Priya = "STF-0000003";
        /// <summary>Jonah "Joe" Ferriday, Primary Care Paramedic (EMS).</summary>
        public const string Jonah = "STF-0000004";
        /// <summary>Ines Halvorsen, Primary Care Paramedic on Probation (EMS).</summary>
        public const string Ines = "STF-0000005";
        /// <summary>Kofi Marchetti, Advanced Care Paramedic (EMS); one cancelled shift.</summary>
        public const string Kofi = "STF-0000006";
        /// <summary>Wren Castellanos, Emergency Dispatcher (EMS).</summary>
        public const string Wren = "STF-0000007";
        /// <summary>Rowan Tibbets, Firefighter, OnLeave (EMS).</summary>
        public const string Rowan = "STF-0000008";
        /// <summary>Anders Kittredge, Detective Sergeant (SEC); holds a Cleared security flag.</summary>
        public const string Anders = "STF-0000010";
        /// <summary>Noor Abernathy, Bylaw Officer, Suspended (SEC); holds the only seeded suspended credential and an Active UnderInvestigation flag.</summary>
        public const string Noor = "STF-0000011";
        /// <summary>Matteo Rasmussen, Fleet Mechanic, Terminated (PTR).</summary>
        public const string Matteo = "STF-0000014";

        /// <summary>The Paramedic Service, the only service with seeded roster entries.</summary>
        public const int ParamedicServiceId = 1;
        /// <summary>Number of services across the 7 departments.</summary>
        public const int ServiceCount = 21;

        /// <summary>EMS requisition for 3 Primary Care Paramedics, Filling, 1 filled.</summary>
        public const string FillingRequisition = "REQ-0000001";

        /// <summary>Credential type id of the Primary Care Paramedic Licence.</summary>
        public const int PrimaryCareParamedicLicence = 1;
        /// <summary>Credential type id of the Certified HR Professional credential (not duty-blocking).</summary>
        public const int CertifiedHrProfessional = 12;
    }

    /// <summary>
    /// Base class for API tests: an HTTPS client on the shared app, JSON settings that match the
    /// API (camelCase, enums as strings), and helpers for requests, RFC 7807 checks and test data.
    /// </summary>
    [Collection(StaffHrApiCollection.Name)]
    public abstract class ApiTestBase
    {
        /// <summary>JSON settings matching the API's.</summary>
        protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
        };

        /// <summary>The running app.</summary>
        protected StaffHrApiFactory Factory { get; }

        /// <summary>Client for the app. HTTPS, so the HTTPS redirection middleware leaves requests alone.</summary>
        protected HttpClient Client { get; }

        /// <summary>Creates the client for a test class.</summary>
        /// <param name="factory">The shared app, supplied by xUnit.</param>
        protected ApiTestBase(StaffHrApiFactory factory)
        {
            Factory = factory;
            Client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        }

        /// <summary>A work email no other test uses.</summary>
        protected static string UniqueEmail() => $"test.{Guid.NewGuid():N}@cmes.example";

        /// <summary>Sends JSON with POST, optionally with an Idempotency-Key header.</summary>
        /// <param name="url">Relative URL.</param>
        /// <param name="body">Object to serialise, or a raw JSON string.</param>
        /// <param name="idempotencyKey">Header value, or null to leave the header out.</param>
        protected Task<HttpResponseMessage> PostAsync(string url, object body, string? idempotencyKey = null) =>
            SendAsync(HttpMethod.Post, url, body, idempotencyKey);

        /// <summary>Sends JSON with PATCH.</summary>
        /// <param name="url">Relative URL.</param>
        /// <param name="body">Object to serialise, or a raw JSON string.</param>
        protected Task<HttpResponseMessage> PatchAsync(string url, object body) =>
            SendAsync(HttpMethod.Patch, url, body, null);

        private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object body, string? idempotencyKey)
        {
            var request = new HttpRequestMessage(method, url)
            {
                Content = body is string raw
                    ? new StringContent(raw, System.Text.Encoding.UTF8, "application/json")
                    : JsonContent.Create(body, options: Json),
            };
            if (idempotencyKey != null) request.Headers.Add("Idempotency-Key", idempotencyKey);
            return Client.SendAsync(request);
        }

        /// <summary>Asserts the status code and reads the body as <typeparamref name="T"/>.</summary>
        /// <typeparam name="T">DTO type.</typeparam>
        /// <param name="response">The response.</param>
        /// <param name="expected">Expected status, 200 by default.</param>
        protected static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
        {
            var text = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == expected, $"Expected {(int)expected}, got {(int)response.StatusCode}: {text}");
            return JsonSerializer.Deserialize<T>(text, Json)!;
        }

        /// <summary>GETs a URL and reads the 200 body as <typeparamref name="T"/>.</summary>
        /// <typeparam name="T">DTO type.</typeparam>
        /// <param name="url">Relative URL.</param>
        protected async Task<T> GetAsync<T>(string url) => await ReadAsync<T>(await Client.GetAsync(url));

        /// <summary>
        /// Asserts an RFC 7807 error: the status code, the problem+json content type, the standard
        /// members (type, title, status, traceId) and, when given, the fields named in 'errors'.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="expected">Expected status.</param>
        /// <param name="errorFields">Fields that must appear in the 'errors' dictionary.</param>
        /// <returns>The parsed problem body, for further checks.</returns>
        protected static async Task<JsonElement> AssertProblemAsync(
            HttpResponseMessage response, HttpStatusCode expected, params string[] errorFields)
        {
            var text = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == expected, $"Expected {(int)expected}, got {(int)response.StatusCode}: {text}");
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            var problem = JsonDocument.Parse(text).RootElement.Clone();
            Assert.Equal((int)expected, problem.GetProperty("status").GetInt32());
            Assert.False(string.IsNullOrEmpty(problem.GetProperty("type").GetString()));
            Assert.False(string.IsNullOrEmpty(problem.GetProperty("title").GetString()));
            Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));

            if (errorFields.Length > 0)
            {
                var errors = problem.GetProperty("errors");
                foreach (var field in errorFields)
                    Assert.True(errors.TryGetProperty(field, out _), $"Expected an error for '{field}' in {errors}");
            }
            return problem;
        }

        /// <summary>Publishes every pending outbox event, as the background publisher would.</summary>
        /// <returns>How many events were published.</returns>
        protected async Task<int> DispatchOutboxAsync()
        {
            var total = 0;
            int published;
            do
            {
                using var scope = Factory.Services.CreateScope();
                published = await scope.ServiceProvider.GetRequiredService<OutboxDispatcher>().DispatchPendingAsync();
                total += published;
            }
            while (published > 0);
            return total;
        }

        /// <summary>The outbox rows whose payload mentions <paramref name="staffNumber"/>.</summary>
        /// <param name="staffNumber">STF-0000000.</param>
        protected async Task<List<OutboxEvent>> OutboxEventsForAsync(string staffNumber)
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CmesDbContext>();
            return await db.OutboxEvents.AsNoTracking()
                .Where(e => e.PayloadJson.Contains(staffNumber))
                .OrderBy(e => e.OccurredAtUtc)
                .ToListAsync();
        }

        /// <summary>The service id of a service, looked up through the API.</summary>
        /// <param name="departmentCode">Department code.</param>
        /// <param name="serviceName">Service name.</param>
        protected async Task<int> ServiceIdAsync(string departmentCode, string serviceName)
        {
            var services = await GetAsync<List<ServiceDto>>($"/departments/{departmentCode}/services");
            return services.Single(s => s.Name == serviceName).ServiceId;
        }

        /// <summary>
        /// Onboards a new employee in HRS, so write tests never change seeded people.
        /// </summary>
        /// <param name="initialAssignment">Optional first role.</param>
        protected async Task<StaffProfileDto> CreateStaffAsync(CreateAssignmentRequest? initialAssignment = null)
        {
            var request = NewStaffRequest() with { InitialAssignment = initialAssignment };
            return await ReadAsync<StaffProfileDto>(await PostAsync("/staff", request), HttpStatusCode.Created);
        }

        /// <summary>A valid onboarding request for a new HRS employee with a unique email.</summary>
        protected static CreateStaffRequest NewStaffRequest() => new(
            FirstName: "Test",
            LastName: "Employee",
            PreferredName: null,
            Email: UniqueEmail(),
            DateOfBirth: new DateOnly(1995, 6, 15),
            Phone: "555-0150",
            CitizenId: null,
            HireDate: StaffHrApiFactory.Today,
            EmploymentType: Domain.EmploymentType.FullTime,
            EmploymentStatus: null,
            HomeDepartmentCode: "HRS",
            InitialAssignment: null);
    }
}
