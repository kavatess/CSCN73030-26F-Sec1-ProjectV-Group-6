/**
 * @file Program.cs
 * @brief Entry point of the Staff/HR ASP.NET Core service.
 *
 * Registers EF Core (SQL Server), the repositories and application services, RFC 7807 error
 * handling and OpenAPI, then builds the request pipeline. Run with `--seed` to apply migrations
 * and load the fictional demo data instead of starting the web server.
 */

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Events;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Auditing;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Seeding;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---- MVC + JSON -----------------------------------------------------------------------------
builder.Services
    .AddControllers(options =>
    {
        // Validation is done explicitly in the application services (422). Without this, MVC would
        // treat every non-nullable reference property as [Required] and answer 400 instead.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options =>
    {
        // Model-binding errors (unreadable JSON, wrong types, empty body) are malformed requests: 400.
        options.InvalidModelStateResponseFactory = context =>
        {
            // Query-string keys arrive as C# property names ("PageSize"); report them as the client
            // sent them. JSON paths ("$.hireDate") and the empty-body key ("") are kept as they are.
            var errors = context.ModelState
                .Where(entry => entry.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    entry => entry.Key.StartsWith('$') ? entry.Key : JsonNamingPolicy.CamelCase.ConvertName(entry.Key),
                    entry => entry.Value!.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage)
                        ? "The value could not be read." : e.ErrorMessage).ToArray());
            // The factory fills in 'type' and 'traceId' the same way as every other error response.
            var problem = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>()
                .CreateValidationProblemDetails(context.HttpContext, context.ModelState,
                    StatusCodes.Status400BadRequest, "Malformed request",
                    detail: "The request could not be read. See 'errors' for details.");
            problem.Errors = errors;
            return new ProblemDetailsResult(problem);
        };
    });
// The OpenAPI generator reads these options, so enums appear as strings in the document too.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---- RFC 7807 errors ------------------------------------------------------------------------
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions.TryAdd("traceId",
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier));
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// ---- OpenAPI (served in Development only, see below) ----------------------------------------
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ApiDocumentTransformer>();
    options.AddOperationTransformer<ProblemExamplesTransformer>();
    options.AddSchemaTransformer<RequestSchemaTransformer>();
});

// ---- Authentication and role-based authorization --------------------------------------------
builder.AddStaffHrAuth();

// ---- Database -------------------------------------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<AuditInterceptor>();
builder.Services.AddDbContext<CmesDbContext>((services, options) => options
    .UseSqlServer(builder.Configuration.GetConnectionString("StaffHr"))
    .AddInterceptors(services.GetRequiredService<AuditInterceptor>()));
builder.Services.AddHealthChecks().AddDbContextCheck<CmesDbContext>("database");

// ---- Application layers ---------------------------------------------------------------------
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<Clock>();

builder.Services.AddScoped<StaffRepository>();
builder.Services.AddScoped<DepartmentRepository>();
builder.Services.AddScoped<AssignmentRepository>();
builder.Services.AddScoped<RosterRepository>();
builder.Services.AddScoped<RequisitionRepository>();
builder.Services.AddScoped<CredentialRepository>();
builder.Services.AddScoped<AuditRepository>();
builder.Services.AddScoped<OutboxRepository>();
builder.Services.AddScoped<SecurityFlagRepository>();
builder.Services.AddScoped<CompensationRepository>();

builder.Services.AddScoped<StaffService>();
builder.Services.AddScoped<DepartmentService>();
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<RosterService>();
builder.Services.AddScoped<RequisitionService>();
builder.Services.AddScoped<CredentialService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<SecurityFlagService>();
builder.Services.AddScoped<EligibilityService>();
builder.Services.AddScoped<TerminationService>();
builder.Services.AddScoped<PayrollService>();

// ---- Integration events (transactional outbox) ----------------------------------------------
builder.Services.Configure<MessagingOptions>(builder.Configuration.GetSection(MessagingOptions.Section));
builder.Services.AddScoped<OutboxWriter>();
builder.Services.AddScoped<OutboxDispatcher>();
var messaging = builder.Configuration.GetSection(MessagingOptions.Section).Get<MessagingOptions>() ?? new MessagingOptions();
if (messaging.ServiceBus.IsConfigured)
    builder.Services.AddSingleton<IEventPublisher, ServiceBusEventPublisher>();
else
    builder.Services.AddSingleton<IEventPublisher, LoggingEventPublisher>();
builder.Services.AddHostedService<OutboxPublisherService>();
builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

// `dotnet run -- --seed`: apply migrations, load demo data, exit.
if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().MigrateAndSeedAsync();
    return;
}

// ---- HTTP pipeline --------------------------------------------------------------------------
app.UseExceptionHandler();

app.UseDefaultFiles();
app.MapStaticAssets();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // /openapi/v1.json
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "CMES Staff/HR API v1");
        options.RoutePrefix = "swagger"; // /swagger
        options.DocumentTitle = "CMES Staff/HR API";
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();

/// <summary>
/// Exposes the entry point so the test project can boot the app with <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program { }
