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
    public DbSet<AbsenceRequest> AbsenceRequests => Set<AbsenceRequest>();
    public DbSet<Approval> Approvals => Set<Approval>();

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

        modelBuilder.Entity<AbsenceRequest>(r =>
        {
            r.HasKey(x => x.Id);
            r.Property(x => x.Reason).IsRequired().HasMaxLength(500);
            r.Property(x => x.Status).HasConversion<string>().IsRequired();
            r.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.OwnerEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            r.HasOne<AbsenceType>()
                .WithMany()
                .HasForeignKey(x => x.AbsenceTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Approval>(a =>
        {
            a.HasKey(x => x.Id);
            a.HasIndex(x => x.RequestId).IsUnique(); // exactly one decision per request (BR-APPR-001)
            a.Property(x => x.Decision).HasConversion<string>().IsRequired();
            a.Property(x => x.Comment).HasMaxLength(1000);
            a.HasOne<AbsenceRequest>()
                .WithMany()
                .HasForeignKey(x => x.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            a.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.DecidedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
