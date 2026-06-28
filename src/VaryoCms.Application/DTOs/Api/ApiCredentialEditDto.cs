namespace VaryoCms.Application.DTOs.Api;

// Populates both Create and Edit forms. Id = 0 means "new credential".
public class ApiCredentialEditDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AuthType { get; set; } = "ApiKey";       // "ApiKey" | "JWT"
    public bool IsActive { get; set; } = true;
    public bool HasApiKey { get; set; }                     // true when an api_key hash is stored; plaintext never returned
    // All tenant content types with IsGranted = true for the ones already covered.
    public List<CredentialContentTypeDto> ContentTypes { get; set; } = new();
}
