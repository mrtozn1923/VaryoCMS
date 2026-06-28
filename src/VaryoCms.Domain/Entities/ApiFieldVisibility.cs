namespace VaryoCms.Domain.Entities;

// Field-level exposure within an API config. No tenant_id / timestamps / IsDeleted (per schema).
public class ApiFieldVisibility
{
    public int Id { get; set; }
    public int ApiConfigurationId { get; set; }
    public int ContentFieldId { get; set; }
    public bool IsVisible { get; set; }
    public string? ResponseKeyAlias { get; set; }   // rename field in JSON response
}
