/**
 * @file OpenApiContractTests.cs
 * @brief Regression tests for the published API contract: routes, error documentation, Swagger UI.
 */

using System.Net;
using System.Text.Json;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>The OpenAPI document other groups review, and the Swagger UI over it.</summary>
    public class OpenApiContractTests(StaffHrApiFactory factory) : ApiTestBase(factory)
    {
        /// <summary>Every endpoint in this release, as "METHOD /path".</summary>
        public static readonly TheoryData<string> Endpoints = new()
        {
            "GET /health",
            "POST /staff", "GET /staff", "GET /staff/{staffNumber}", "PATCH /staff/{staffNumber}",
            "GET /departments", "GET /departments/{code}/services", "GET /departments/headcount",
            "POST /staff/{staffNumber}/assignments", "GET /staff/{staffNumber}/assignments", "PATCH /assignments/{id}",
            "GET /rosters", "POST /rosters/entries", "DELETE /rosters/entries/{id}", "GET /rosters/headcount", "GET /rosters/on-duty",
            "POST /requisitions", "GET /requisitions", "GET /requisitions/{number}", "PATCH /requisitions/{number}",
            "GET /credentials", "GET /credentials/types", "GET /credentials/expiring", "POST /staff/{staffNumber}/credentials", "PATCH /credentials/{id}",
            "GET /audit",
            "PUT /staff/{staffNumber}/security-flags/{flagType}", "GET /staff/{staffNumber}/security-flags", "GET /security-flags",
        };

        private async Task<JsonElement> DocumentAsync()
        {
            using var doc = JsonDocument.Parse(await Client.GetStringAsync("/openapi/v1.json"));
            return doc.RootElement.Clone();
        }

        private static JsonElement Operation(JsonElement doc, string endpoint)
        {
            var parts = endpoint.Split(' ');
            return doc.GetProperty("paths").GetProperty(parts[1]).GetProperty(parts[0].ToLowerInvariant());
        }

        [Theory]
        [MemberData(nameof(Endpoints))]
        public async Task Document_ListsEveryEndpoint(string endpoint)
        {
            var doc = await DocumentAsync();
            Assert.True(Operation(doc, endpoint).TryGetProperty("responses", out _), endpoint);
        }

        [Theory]
        [MemberData(nameof(Endpoints))]
        public async Task Document_ErrorResponsesAreProblemJsonWithAnExample(string endpoint)
        {
            var responses = Operation(await DocumentAsync(), endpoint).GetProperty("responses");

            foreach (var response in responses.EnumerateObject().Where(r => r.Name.StartsWith('4')))
            {
                var content = response.Value.GetProperty("content");
                Assert.Equal(["application/problem+json"], content.EnumerateObject().Select(c => c.Name));

                var example = content.GetProperty("application/problem+json").GetProperty("example");
                Assert.Equal(int.Parse(response.Name), example.GetProperty("status").GetInt32());
                Assert.False(string.IsNullOrEmpty(example.GetProperty("detail").GetString()));
                if (response.Name is "400" or "422")
                    Assert.Equal(JsonValueKind.Object, example.GetProperty("errors").ValueKind);
            }
        }

        [Theory]
        [InlineData("POST /staff", "409")]
        [InlineData("POST /staff", "422")]
        [InlineData("GET /staff/{staffNumber}", "404")]
        [InlineData("PATCH /staff/{staffNumber}", "409")]
        [InlineData("POST /rosters/entries", "409")]
        [InlineData("POST /requisitions", "400")]
        [InlineData("POST /requisitions", "409")]
        [InlineData("PATCH /credentials/{id}", "404")]
        public async Task Document_DescribesTheKeyErrorCases(string endpoint, string status)
        {
            var responses = Operation(await DocumentAsync(), endpoint).GetProperty("responses");
            Assert.True(responses.TryGetProperty(status, out _), $"{endpoint} should document {status}");
        }

        [Fact]
        public async Task Document_IdempotencyKeyHeaderIsRequired()
        {
            var parameters = Operation(await DocumentAsync(), "POST /requisitions").GetProperty("parameters");

            var header = parameters.EnumerateArray().Single(p => p.GetProperty("name").GetString() == "Idempotency-Key");
            Assert.Equal("header", header.GetProperty("in").GetString());
            Assert.True(header.GetProperty("required").GetBoolean());
        }

        [Fact]
        public async Task Document_NoSchemaExposesSalary()
        {
            var schemas = (await DocumentAsync()).GetProperty("components").GetProperty("schemas");

            // Descriptions may mention salary ("Never includes salary"); no schema or field may carry it.
            var names = schemas.EnumerateObject().Select(s => s.Name).ToList();
            foreach (var schema in schemas.EnumerateObject())
            {
                if (schema.Value.TryGetProperty("properties", out var properties))
                    names.AddRange(properties.EnumerateObject().Select(p => p.Name));
            }

            Assert.NotEmpty(names);
            Assert.DoesNotContain(names, n => n.Contains("salary", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(names, n => n.Contains("payBand", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(names, n => n.Contains("compensation", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Document_RequestSchemasListOnlyTrulyRequiredFields()
        {
            var schemas = (await DocumentAsync()).GetProperty("components").GetProperty("schemas");

            var create = schemas.GetProperty("CreateStaffRequest").GetProperty("required").EnumerateArray().Select(e => e.GetString());
            Assert.Equal(["dateOfBirth", "email", "employmentType", "firstName", "hireDate", "homeDepartmentCode", "lastName"], create.Order());

            // PATCH bodies are partial: nothing is required.
            var patch = schemas.GetProperty("UpdateStaffRequest");
            Assert.True(!patch.TryGetProperty("required", out var required) || required.GetArrayLength() == 0);
        }

        [Fact]
        public async Task Document_EnumsAreStrings()
        {
            var status = (await DocumentAsync()).GetProperty("components").GetProperty("schemas").GetProperty("EmploymentStatus");
            Assert.Equal(["Active", "Probation", "OnLeave", "Suspended", "Terminated"],
                status.GetProperty("enum").EnumerateArray().Select(e => e.GetString()));
        }

        [Fact]
        public async Task SwaggerUi_IsServedInDevelopment()
        {
            var response = await Client.GetAsync("/swagger/index.html");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        }
    }
}
