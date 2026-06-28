using VaryoCms.Application.Interfaces;

namespace VaryoCms.Infrastructure.Security;

// BCrypt-backed password hashing (BCrypt.Net-Next). Salt is embedded in the hash output.
public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;   // stored hash is malformed / not a BCrypt hash
        }
    }
}
