using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using VacaFlow.Api.Extensions;
using VacaFlow.Api.Middleware;
using VacaFlow.Api.Slices.Auth;
using VacaFlow.Application.Validation;
using VacaFlow.Domain.Ports;
using VacaFlow.Infrastructure.Persistence;
using VacaFlow.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// 0. JSON — accept/emit enums by name (e.g. Role "Employee"/"Manager")
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// 1. EF Core + SQLite
builder.Services.AddDbContext<VacaFlowDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Domain ports -> Infrastructure adapters
builder.Services.AddScoped<IPasswordHasher, BCryptHasher>();

// 3. Validators (one IValidator<T> per command type)
builder.Services.AddScoped<IValidator<RegisterSlice.RegisterCommand>, RegisterSlice.RegisterValidator>();

// 4. Validation pipeline
builder.Services.AddScoped<ValidationPipelineInvoker>();

// 5. Cookie authentication (JWT is prohibited for the MVP)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

// 6. Anti-forgery
builder.Services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");

// 7. RFC 7807 problem-details exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// 8. Apply migrations and seed before the API accepts requests (FR-AUTH-009, AC-006)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VacaFlowDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await db.Database.MigrateAsync();
    await SeedDataInitializer.SeedAsync(db, hasher);
}

// 9. Middleware pipeline (order matters)
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// 10. Slice endpoints
app.RegisterAllSlices();

app.Run();

// Exposed for WebApplicationFactory<Program> in the integration tests.
public partial class Program;
