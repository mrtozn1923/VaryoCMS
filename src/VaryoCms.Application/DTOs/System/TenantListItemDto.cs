namespace VaryoCms.Application.DTOs.System;

public class TenantListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
    public int ContentTypeCount { get; set; }
}
