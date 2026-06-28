namespace VaryoCms.Application.DTOs.Api;

// Auth material pulled from a public API request: the X-API-Key header and/or the Bearer token.
public record ApiCredentials(string? ApiKey, string? BearerToken);
