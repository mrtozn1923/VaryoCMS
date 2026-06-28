namespace VaryoCms.Application.Interfaces;

// Hashes and verifies passwords. Implemented by Infrastructure (BCrypt).
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
