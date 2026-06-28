using VaryoCms.Application.DTOs.Media;

namespace VaryoCms.Web.ViewModels;

public class MediaLibraryViewModel
{
    public IReadOnlyList<MediaAssetDto> Items { get; set; } = Array.Empty<MediaAssetDto>();
    public string? MediaType { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}
