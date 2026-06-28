namespace VaryoCms.Application.Common;

// Minimal tenant projection used during request-time tenant resolution.
public record TenantInfo(int Id, string Slug, string Name, string DefaultLanguageCode);
