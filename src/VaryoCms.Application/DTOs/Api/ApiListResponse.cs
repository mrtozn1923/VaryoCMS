namespace VaryoCms.Application.DTOs.Api;

public class ApiListResponse
{
    public IReadOnlyList<ApiItemDto> Data { get; set; } = Array.Empty<ApiItemDto>();
    public ApiPaginationDto Pagination { get; set; } = new();
}

public class ApiPaginationDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}
