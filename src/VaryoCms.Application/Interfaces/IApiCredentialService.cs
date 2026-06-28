using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Api;

namespace VaryoCms.Application.Interfaces;

// Manages shared API credentials (named ApiKey or JWT) that cover one or more content types.
public interface IApiCredentialService
{
    Task<Result<IReadOnlyList<ApiCredentialListItemDto>>> GetListAsync(CancellationToken ct = default);

    // Returns the credential with all content types (IsGranted = true for covered ones).
    // When id = 0 (or null), returns a blank "create" form with all content types IsGranted = false.
    Task<Result<ApiCredentialEditDto>> GetForEditAsync(int? id, CancellationToken ct = default);

    // Creates or updates the credential and its content type grants.
    // Always returns the credential id (new or existing).
    // For a new ApiKey credential: PlaintextKey contains the key (shown once). Otherwise empty.
    Task<Result<ApiCredentialSaveResult>> SaveAsync(SaveApiCredentialRequest request, CancellationToken ct = default);

    // Rotates the API key for an existing ApiKey credential. Returns plaintext once.
    Task<Result<string>> RotateApiKeyAsync(int credentialId, CancellationToken ct = default);

    // Issues a new CMS-signed JWT covering all content types granted to this credential.
    // JWT is stateless — revocation requires the user to let it expire or stop using it.
    Task<Result<string>> IssueJwtAsync(int credentialId, CancellationToken ct = default);

    Task<Result> DeleteAsync(int credentialId, CancellationToken ct = default);
}
