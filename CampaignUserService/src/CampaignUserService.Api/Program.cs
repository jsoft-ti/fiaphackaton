using Asp.Versioning;
using Asp.Versioning.Builder;
using CampaignUserService.Api.Authorization;
using CampaignUserService.Api.Endpoints;
using CampaignUserService.Api.Extensions;
using CampaignUserService.Api.Middleware;
using CampaignUserService.Application;
using CampaignUserService.Infrastructure;
using CampaignUserService.Infrastructure.Persistence.Seed;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog (structured logging: request/response, exceptions, performance) ----------
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithExceptionDetails()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .WriteTo.Console()
        .WriteTo.File(
            "logs/campaign-user-service-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14);
});

// ---------- Services ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCampaignUserAuthorizationPolicies();

builder.Services.AddApiVersioningConfig();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddApiRateLimiting();
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddApplicationHealthChecks(builder.Configuration);

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ---------- Database migration + seed (roles, initial GestorOng administrator) ----------
if (builder.Configuration.GetValue("Database:AutoMigrateAndSeed", true))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}

// ---------- Middleware pipeline ----------
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors(CorsExtensions.PolicyName);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CampaignUserService API v1");
    options.RoutePrefix = "swagger";
});

// ---------- Endpoints ----------
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

app.MapAuthEndpoints(apiVersionSet);
app.MapUsersEndpoints(apiVersionSet);
app.MapRolesEndpoints(apiVersionSet);

app.MapApplicationHealthChecks();

app.Run();

// Exposes the implicit Program class to WebApplicationFactory<Program> in integration tests.
public partial class Program
{
}
