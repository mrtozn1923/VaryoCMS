namespace VaryoCms.Application.DTOs.Api;

// Represents a content type in the credential form, with a flag indicating whether this credential covers it.
public class CredentialContentTypeDto
{
    public int ContentTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
}
