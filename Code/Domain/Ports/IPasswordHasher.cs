namespace VacaFlow.Domain.Ports;

/// <summary>
/// Port for password hashing. The Domain never depends on a concrete hashing library;
/// the Infrastructure layer supplies the adapter (BR-SEC-001).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string plainTextPassword);
    bool Verify(string plainTextPassword, string hashedPassword);
}
