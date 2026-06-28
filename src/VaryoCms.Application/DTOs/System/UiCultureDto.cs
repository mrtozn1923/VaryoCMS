namespace VaryoCms.Application.DTOs.System;

public class UiCultureDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}
