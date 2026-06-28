namespace VaryoCms.Application.DTOs.Api;

public class ApiItemDto
{
    public int Id { get; set; }
    public string? Slug { get; set; }
    public Dictionary<string, object?> Fields { get; set; } = new();
    public ApiItemMetaDto Meta { get; set; } = new();
}

public class ApiItemMetaDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Status { get; set; } = null!;
    public string Language { get; set; } = null!;
}
