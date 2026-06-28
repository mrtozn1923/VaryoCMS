namespace VaryoCms.Application.Interfaces;

// Issues and validates the CMS-signed Bearer tokens used by the public API's JWT auth mode.
// Tokens are self-contained (signed with an app-level key); no per-token DB storage.
public interface IJwtTokenService
{
    // Signs a token scoped to (tenant, one-or-more content types) with the given lifetime.
    // Multiple content type slugs each become a separate "ct" claim in the token.
    string IssueToken(string tenantSlug, IReadOnlyCollection<string> contentTypeSlugs, TimeSpan lifetime);

    // True if the token's signature, issuer and lifetime are valid AND its tenant claim matches
    // AND one of its "ct" claims matches contentTypeSlug (membership check, not equality).
    bool ValidateToken(string token, string tenantSlug, string contentTypeSlug);

    // Validates signature/issuer/lifetime and returns all claims without requiring a specific CT slug.
    // Used by the API explorer to enumerate all content types a token is authorized for.
    (bool IsValid, string? TenantSlug, IReadOnlyList<string> ContentTypeSlugs) ExtractClaims(string token);
}
