namespace VaryoCms.Application.DTOs.System;

// Slug is intentionally absent — it's immutable after creation (changing it breaks subdomain routing).
public class UpdateTenantRequest
{
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
}
