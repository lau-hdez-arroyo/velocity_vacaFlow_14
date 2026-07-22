namespace VacaFlow.Domain.Entities;

/// <summary>
/// Seeded reference catalog (Vacation, Personal Leave, Sick Leave). Not user-maintainable (BR-DATA-001).
/// </summary>
public class AbsenceType
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
}
