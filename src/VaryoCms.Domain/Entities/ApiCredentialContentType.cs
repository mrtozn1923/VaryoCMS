namespace VaryoCms.Domain.Entities;

// Many-to-many junction: which content types a credential covers.
public class ApiCredentialContentType
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ApiCredentialId { get; set; }
    public int ContentTypeId { get; set; }
}
