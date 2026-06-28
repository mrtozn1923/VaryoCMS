namespace VaryoCms.Domain.Entities;

public class MediaAsset
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string FileName { get; set; } = null!;
    public string OriginalName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string MediaType { get; set; } = null!;   // image | video | audio | file
    public string? MimeType { get; set; }
    public long? FileSizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public decimal? DurationSecs { get; set; }
    public string? AltText { get; set; }
    public string? MetadataJson { get; set; }
    public int? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
