using VaryoCms.Infrastructure.Security;

namespace VaryoCms.Tests.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_then_verify_succeeds()
    {
        var hash = _hasher.Hash("Secret123!");
        Assert.StartsWith("$2", hash);
        Assert.True(_hasher.Verify("Secret123!", hash));
    }

    [Fact]
    public void Verify_wrong_password_fails()
    {
        var hash = _hasher.Hash("Secret123!");
        Assert.False(_hasher.Verify("wrong", hash));
    }

    [Fact]
    public void Verify_malformed_hash_returns_false()
    {
        Assert.False(_hasher.Verify("anything", "not-a-bcrypt-hash"));
    }

    [Fact]
    public void Hashes_are_salted_and_differ()
    {
        Assert.NotEqual(_hasher.Hash("same"), _hasher.Hash("same"));
    }
}
