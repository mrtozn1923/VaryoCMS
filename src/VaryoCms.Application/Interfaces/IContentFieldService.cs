using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.ContentField;

namespace VaryoCms.Application.Interfaces;

public interface IContentFieldService
{
    Task<Result<IReadOnlyList<ContentFieldDto>>> GetByContentTypeAsync(int contentTypeId, CancellationToken ct = default);
    Task<Result<ContentFieldDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(CreateContentFieldRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, UpdateContentFieldRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result> ReorderAsync(int contentTypeId, ReorderFieldsRequest request, CancellationToken ct = default);
}
