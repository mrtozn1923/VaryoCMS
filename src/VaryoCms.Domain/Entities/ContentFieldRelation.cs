namespace VaryoCms.Domain.Entities;

// Link row for Relation / MultiRelation fields. No timestamps / IsDeleted (per schema).
public class ContentFieldRelation
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int SourceItemId { get; set; }
    public int SourceFieldId { get; set; }
    public int TargetItemId { get; set; }
    public int SortOrder { get; set; }
}
