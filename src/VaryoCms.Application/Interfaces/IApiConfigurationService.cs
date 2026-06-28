using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Api;

namespace VaryoCms.Application.Interfaces;

// Manages per-content-type API configuration (enabled, public/protected, flags, rate limit, field visibility).
// Credential management (ApiKey / JWT generation) has moved to IApiCredentialService.
public interface IApiConfigurationService
{
    Task<Result<IReadOnlyList<ApiConfigListItemDto>>> GetListAsync(CancellationToken ct = default);
    Task<Result<ApiConfigEditDto>> GetForEditAsync(int contentTypeId, CancellationToken ct = default);
    Task<Result> SaveAsync(SaveApiConfigRequest request, CancellationToken ct = default);
}
