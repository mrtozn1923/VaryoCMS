using System.ComponentModel.DataAnnotations;
using VaryoCms.Application.DTOs.Api;
using VaryoCms.Domain.Enums;

namespace VaryoCms.Web.ViewModels;

public class ApiCredentialFormViewModel
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string AuthType { get; set; } = "ApiKey";   // "ApiKey" | "JWT"
    public bool IsActive { get; set; } = true;
    public bool HasApiKey { get; set; }

    // All tenant content types; IsGranted = true for the ones already covered.
    public List<CredentialContentTypeDto> ContentTypes { get; set; } = new();

    public SaveApiCredentialRequest ToRequest() => new()
    {
        Id = Id,
        Name = Name?.Trim() ?? string.Empty,
        AuthType = AuthType,
        IsActive = IsActive,
        ContentTypeIds = ContentTypes
            .Where(c => c.IsGranted)
            .Select(c => c.ContentTypeId)
            .ToList()
    };

    public static ApiCredentialFormViewModel FromDto(ApiCredentialEditDto d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        AuthType = d.AuthType,
        IsActive = d.IsActive,
        HasApiKey = d.HasApiKey,
        ContentTypes = d.ContentTypes
    };
}
