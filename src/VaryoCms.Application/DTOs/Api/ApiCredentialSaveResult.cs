namespace VaryoCms.Application.DTOs.Api;

/// <summary>
/// Returned by IApiCredentialService.SaveAsync.
/// CredentialId is always set. PlaintextKey is non-empty only for a newly-created ApiKey credential
/// (shown once; after that only the BCrypt hash is stored).
/// </summary>
public record ApiCredentialSaveResult(int CredentialId, string PlaintextKey);
