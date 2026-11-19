/**
 * @file JwtAuthenticationTests.cs
 * @brief Regression tests for Entra ID bearer-token validation, using locally signed test tokens.
 */

using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>
    /// Runs the app in EntraId mode. Instead of downloading Entra ID's signing keys, the test trusts
    /// one local key and issuer, so it can mint valid, expired and wrongly addressed tokens.
    /// </summary>
    [Collection(StaffHrApiCollection.Name)]
    public class JwtAuthenticationTests
    {
        private const string Issuer = "https://login.microsoftonline.com/test-tenant/v2.0";
        private const string Audience = "api://cmes-staff-hr";
        private static readonly SymmetricSecurityKey Key = new(Guid.NewGuid().ToByteArray().Concat(Guid.NewGuid().ToByteArray()).ToArray());

        private readonly HttpClient _client;

        /// <summary>Starts a copy of the app in EntraId mode against the shared test database.</summary>
        /// <param name="factory">The shared app.</param>
        public JwtAuthenticationTests(StaffHrApiFactory factory)
        {
            var app = factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Authentication:Mode", "EntraId");
                builder.UseSetting("Authentication:EntraId:Audience", Audience);
                builder.ConfigureTestServices(services =>
                    services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, jwt =>
                    {
                        jwt.Authority = null;
                        jwt.Configuration = new OpenIdConnectConfiguration();
                        jwt.TokenValidationParameters.ValidIssuers = [Issuer];
                        jwt.TokenValidationParameters.IssuerSigningKey = Key;
                    }));
            });
            _client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        }

        private static string Token(string[] roles, string audience = Audience, SecurityKey? key = null, DateTime? expires = null)
        {
            var now = DateTime.UtcNow;
            var claims = new List<Claim> { new("preferred_username", "bob@cmes.example"), new("oid", Guid.NewGuid().ToString()) };
            claims.AddRange(roles.Select(r => new Claim("roles", r)));
            return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                Audience = audience,
                Subject = new ClaimsIdentity(claims),
                NotBefore = now.AddMinutes(-10),
                IssuedAt = now.AddMinutes(-10),
                Expires = expires ?? now.AddMinutes(30),
                SigningCredentials = new SigningCredentials(key ?? Key, SecurityAlgorithms.HmacSha256),
            });
        }

        private Task<HttpResponseMessage> GetAsync(string url, string? token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return _client.SendAsync(request);
        }

        [Fact]
        public async Task ValidTokenWithRole_IsAccepted()
        {
            var response = await GetAsync("/departments", Token([StaffHrRoles.Read]));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task NoToken_Returns401WithBearerChallenge()
        {
            var response = await GetAsync("/departments", null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            Assert.Contains(response.Headers.WwwAuthenticate, h => h.Scheme == "Bearer");
        }

        [Fact]
        public async Task ExpiredToken_Returns401()
        {
            var expired = Token([StaffHrRoles.Read], expires: DateTime.UtcNow.AddMinutes(-6));
            Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync("/departments", expired)).StatusCode);
        }

        [Fact]
        public async Task TokenForAnotherApi_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await GetAsync("/departments", Token([StaffHrRoles.Read], audience: "api://some-other-service"))).StatusCode);
        }

        [Fact]
        public async Task TokenSignedWithAnotherKey_Returns401()
        {
            var forged = Token([StaffHrRoles.Admin], key: new SymmetricSecurityKey(new byte[32]));
            Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync("/departments", forged)).StatusCode);
        }

        [Fact]
        public async Task ValidTokenWithoutTheRole_Returns403()
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync("/headcounts/payroll", Token([StaffHrRoles.Admin]))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await GetAsync("/headcounts/payroll", Token([StaffHrRoles.Payroll]))).StatusCode);
        }

        [Fact]
        public async Task DevelopmentHeaders_AreIgnoredInEntraIdMode()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/departments");
            request.Headers.Add("X-Dev-Roles", StaffHrRoles.Admin);
            Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(request)).StatusCode);
        }
    }
}
