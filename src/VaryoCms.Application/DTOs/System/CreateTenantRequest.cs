namespace VaryoCms.Application.DTOs.System;

public class CreateTenantRequest
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string DefaultLanguageCode { get; set; } = null!;
    public string DefaultLanguageName { get; set; } = null!;
    public string FirstAdminEmail { get; set; } = null!;
    public string FirstAdminPassword { get; set; } = null!;
    public string? FirstAdminFullName { get; set; }
}
