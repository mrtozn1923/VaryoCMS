namespace VaryoCms.Application.DTOs.System;

public class TenantEditDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;   // read-only in the UI (immutable)
    public bool IsActive { get; set; }
}
