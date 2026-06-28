using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.ApiDocs;

namespace VaryoCms.Application.Interfaces;

public interface IApiDocService
{
    Task<Result<ApiDocManifestDto>> GetManifestAsync(string tenantSlug, string credential, CancellationToken ct);
}
