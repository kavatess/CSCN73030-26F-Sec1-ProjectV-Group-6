using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests
{
    public class OpenApiDocumentTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public OpenApiDocumentTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task OpenApiDocument_IsServedInDevelopment()
        {
            var client = _factory
                .WithWebHostBuilder(builder => builder.UseEnvironment("Development"))
                .CreateClient();

            var response = await client.GetAsync("/openapi/v1.json");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task OpenApiDocument_IsNotServedInProduction()
        {
            var client = _factory
                .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
                .CreateClient();

            var response = await client.GetAsync("/openapi/v1.json");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
