using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VaryoCms.Application.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace VaryoCms.Infrastructure.Security;

// HS256 CMS-signed tokens for the public API's JWT auth mode. Key/issuer come from configuration.
public class JwtTokenService : IJwtTokenService
{
    private const string TenantClaim = "tenant";
    private const string ContentTypeClaim = "ct";

    private readonly SymmetricSecurityKey _key;
    private readonly string _issuer;

    public JwtTokenService(string signingKey, string issuer)
    {
        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be configured and at least 32 bytes long.");
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        _issuer = issuer;
    }

    public string IssueToken(string tenantSlug, IReadOnlyCollection<string> contentTypeSlugs, TimeSpan lifetime)
    {
        // Each content type slug becomes a separate "ct" claim; token covers all of them.
        var claims = new List<Claim> { new Claim(TenantClaim, tenantSlug) };
        foreach (string slug in contentTypeSlugs)
            claims.Add(new Claim(ContentTypeClaim, slug));

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _issuer,
            claims: claims,
            notBefore: now,
            expires: now.Add(lifetime),
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool ValidateToken(string token, string tenantSlug, string contentTypeSlug)
    {
        // Validates signature/issuer/lifetime, then checks tenant claim (equality) and
        // content type claim (membership — any one of the "ct" claims must match).
        if (string.IsNullOrWhiteSpace(token)) return false;

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
            bool tenantMatch = string.Equals(
                principal.FindFirst(TenantClaim)?.Value, tenantSlug, StringComparison.OrdinalIgnoreCase);
            bool contentTypeMatch = principal.FindAll(ContentTypeClaim)
                .Any(c => string.Equals(c.Value, contentTypeSlug, StringComparison.OrdinalIgnoreCase));
            return tenantMatch && contentTypeMatch;
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return false;
        }
    }

    public (bool IsValid, string? TenantSlug, IReadOnlyList<string> ContentTypeSlugs) ExtractClaims(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (false, null, Array.Empty<string>());

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
            string? tenant = principal.FindFirst(TenantClaim)?.Value;
            var ctSlugs = principal.FindAll(ContentTypeClaim).Select(c => c.Value).ToList();
            return (true, tenant, ctSlugs);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return (false, null, Array.Empty<string>());
        }
    }
}
