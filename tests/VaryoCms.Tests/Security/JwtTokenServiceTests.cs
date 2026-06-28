using VaryoCms.Infrastructure.Security;

namespace VaryoCms.Tests.Security;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _jwt =
        new("unit-test-signing-key-at-least-32-bytes-long!!", "VaryoCms");

    [Fact]
    public void Token_validates_for_matching_tenant_and_content_type()
    {
        var token = _jwt.IssueToken("acme", new[] { "blog" }, TimeSpan.FromMinutes(5));
        Assert.True(_jwt.ValidateToken(token, "acme", "blog"));
    }

    [Fact]
    public void Token_rejected_for_different_content_type()
    {
        var token = _jwt.IssueToken("acme", new[] { "blog" }, TimeSpan.FromMinutes(5));
        Assert.False(_jwt.ValidateToken(token, "acme", "news"));
    }

    [Fact]
    public void Token_rejected_for_different_tenant()
    {
        var token = _jwt.IssueToken("acme", new[] { "blog" }, TimeSpan.FromMinutes(5));
        Assert.False(_jwt.ValidateToken(token, "other", "blog"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage.token.value")]
    public void Garbage_token_rejected(string token)
    {
        Assert.False(_jwt.ValidateToken(token, "acme", "blog"));
    }

    [Fact]
    public void Token_signed_with_other_key_rejected()
    {
        var other = new JwtTokenService("a-completely-different-signing-key-32bytes!!", "VaryoCms");
        var token = other.IssueToken("acme", new[] { "blog" }, TimeSpan.FromMinutes(5));
        Assert.False(_jwt.ValidateToken(token, "acme", "blog"));
    }

    [Fact]
    public void Short_signing_key_is_rejected_at_construction()
    {
        Assert.Throws<InvalidOperationException>(() => new JwtTokenService("too-short", "VaryoCms"));
    }

    // New: multi-content-type token
    [Fact]
    public void Multi_ct_token_validates_for_any_covered_content_type()
    {
        var token = _jwt.IssueToken("acme", new[] { "blog", "news", "products" }, TimeSpan.FromMinutes(5));
        Assert.True(_jwt.ValidateToken(token, "acme", "blog"));
        Assert.True(_jwt.ValidateToken(token, "acme", "news"));
        Assert.True(_jwt.ValidateToken(token, "acme", "products"));
    }

    [Fact]
    public void Multi_ct_token_rejected_for_uncovered_content_type()
    {
        var token = _jwt.IssueToken("acme", new[] { "blog", "news" }, TimeSpan.FromMinutes(5));
        Assert.False(_jwt.ValidateToken(token, "acme", "products"));
    }
}
