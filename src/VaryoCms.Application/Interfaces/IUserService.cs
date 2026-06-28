using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.User;

namespace VaryoCms.Application.Interfaces;

public interface IUserService
{
    Task<Result<PagedResult<UserListItemDto>>> GetListAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<UserEditDto>> GetForEditAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);

    Task<Result<UserPermissionsDto>> GetPermissionsAsync(int userId, CancellationToken ct = default);
    Task<Result> SavePermissionsAsync(int userId, SaveUserPermissionsRequest request, CancellationToken ct = default);
}
