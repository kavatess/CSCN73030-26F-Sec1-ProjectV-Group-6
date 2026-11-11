/**
 * @file Program.cs
 * @brief Entry point of the Staff/HR ASP.NET Core service.
 *
 * Registers MVC controllers and OpenAPI, then builds the request pipeline: static files,
 * OpenAPI (Development only), HTTPS redirection, authorization, controller routing, and a
 * fallback to the React client's index.html.
 */
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Register the database context. The connection string comes from the settings files.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("StaffHr")));
    
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();

/// <summary>
/// Exposes the entry point so the test project can boot the app with <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program { }
