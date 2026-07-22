using VacaFlow.Domain.Ports;

namespace VacaFlow.Infrastructure.Security;

/// <summary>BCrypt adapter for <see cref="IPasswordHasher"/> (BR-SEC-001).</summary>
public class BCryptHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainTextPassword)
        => BCrypt.Net.BCrypt.HashPassword(plainTextPassword, WorkFactor);

    public bool Verify(string plainTextPassword, string hashedPassword)
        => BCrypt.Net.BCrypt.Verify(plainTextPassword, hashedPassword);
}
