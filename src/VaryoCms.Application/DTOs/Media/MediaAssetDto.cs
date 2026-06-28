namespace VaryoCms.Application.DTOs.Media;

public class MediaAssetDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = null!;
    public string OriginalName { get; set; } = null!;
    public string Url { get; set; } = null!;          // web-relative path, e.g. /uploads/1/abc.jpg
    public string MediaType { get; set; } = null!;     // image | video | audio | file
    public string? MimeType { get; set; }
    public long? FileSizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? AltText { get; set; }
    public DateTime CreatedAt { get; set; }
}
