namespace VaryoCms.Application.DTOs.Api;

public class ApiCredentialListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AuthType { get; set; } = string.Empty;   // "ApiKey" | "JWT"
    public bool IsActive { get; set; }
    public int GrantedCount { get; set; }                  // number of covered content types
    public DateTime CreatedAt { get; set; }
}
