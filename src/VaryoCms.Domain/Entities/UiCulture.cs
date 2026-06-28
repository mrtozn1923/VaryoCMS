namespace VaryoCms.Domain.Entities;

// A selectable admin-UI culture (global, no tenant). Read model for management.
public record UiCulture(string Code, string Name, bool IsDefault, bool IsActive);
