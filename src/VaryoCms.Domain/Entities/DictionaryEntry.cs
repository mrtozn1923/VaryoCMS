namespace VaryoCms.Domain.Entities;

public class DictionaryEntry
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string KeyName { get; set; } = null!;   // e.g. 'nav.home', 'btn.save'
    public string? Category { get; set; }          // 'navigation', 'buttons', 'errors'
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
