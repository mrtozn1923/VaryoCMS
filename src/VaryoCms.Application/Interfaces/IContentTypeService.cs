using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.ContentType;

namespace VaryoCms.Application.Interfaces;

public interface IContentTypeService
{
    Task<Result<IReadOnlyList<ContentTypeDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<ContentTypeDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(CreateContentTypeRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, UpdateContentTypeRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
