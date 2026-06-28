namespace VaryoCms.Application.DTOs.Navigation;

// A content type the current user may read — used to build the left sidebar menu.
public class AccessibleContentTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Icon { get; set; }
    public int? ParentId { get; set; }
}
