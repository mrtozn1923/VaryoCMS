using VaryoCms.Application.DTOs.User;

namespace VaryoCms.Web.ViewModels;

public class UserListViewModel
{
    public IReadOnlyList<UserListItemDto> Items { get; set; } = Array.Empty<UserListItemDto>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}
