using VaryoCms.Domain.Enums;

namespace VaryoCms.Domain.Entities;

// Shared, named API credential — one per tenant; covers multiple content types via ApiCredentialContentType.
public class ApiCredential
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ApiAuthType AuthType { get; set; }       // ApiKey | JWT (None is not a valid credential type)
    public string? ApiKey { get; set; }             // BCrypt hash (ApiKey only); null for JWT (stateless)
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
