using VacaFlow.Domain.Enums;

namespace VacaFlow.Domain.Entities;

/// <summary>
/// The single user/identity entity. A registered account with a role; a Manager
/// reference (<see cref="ManagerId"/>) is set only via seed/controlled setup (BR-DATA-002),
/// never at self-registration.
/// </summary>
public class Employee
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required Role Role { get; set; }
    public Guid? ManagerId { get; set; }
}
