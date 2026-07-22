using Microsoft.EntityFrameworkCore;
using VacaFlow.Domain.Entities;

namespace VacaFlow.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext. All mapping lives here via the Fluent API — Domain entities carry
/// no ORM attributes (persistence ignorance). US-001 maps Employees + AbsenceTypes;
/// later stories add AbsenceRequests/Approvals via their own additive migrations.
/// </summary>
public class VacaFlowDbContext : DbContext
{
    public VacaFlowDbContext(DbContextOptions<VacaFlowDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AbsenceType> AbsenceTypes => Set<AbsenceType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).IsRequired();
            e.Property(x => x.Email).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().IsRequired();
            e.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.ManagerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AbsenceType>(a =>
        {
            a.HasKey(x => x.Id);
            a.Property(x => x.Name).IsRequired();
            a.HasIndex(x => x.Name).IsUnique();
        });
    }
}
