using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VacaFlow.Infrastructure.Persistence;

namespace Api.Integration.Tests;

/// <summary>
/// Boots the real API in-process against an isolated, throwaway SQLite file so the startup
/// migrate + seed pipeline runs exactly as in production. One DB per factory instance.
/// </summary>
public class VacaFlowWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"vacaflow-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<VacaFlowDbContext>>();
            services.RemoveAll<VacaFlowDbContext>();
            services.AddDbContext<VacaFlowDbContext>(options => options.UseSqlite($"Data Source={_dbPath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { /* best-effort cleanup */ }
        }
    }
}
