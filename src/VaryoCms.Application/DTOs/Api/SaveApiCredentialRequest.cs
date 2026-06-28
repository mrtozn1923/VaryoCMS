namespace VaryoCms.Application.DTOs.Api;

public class SaveApiCredentialRequest
{
    public int Id { get; set; }                             // 0 = create; >0 = update
    public string Name { get; set; } = string.Empty;
    public string AuthType { get; set; } = "ApiKey";       // "ApiKey" | "JWT"
    public bool IsActive { get; set; } = true;
    public List<int> ContentTypeIds { get; set; } = new(); // must contain at least one
}
